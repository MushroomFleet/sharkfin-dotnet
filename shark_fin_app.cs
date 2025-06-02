using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace SharkFinCompanion
{
    // Settings configuration class
    public class SharkSettings
    {
        public BehaviorSettings BehaviorSettings { get; set; } = new BehaviorSettings();
        public double SpeedMultiplier { get; set; } = 1.0;
        public bool MultipleSharkEnabled { get; set; } = false;
        public bool AutoStartEnabled { get; set; } = false;
    }
    
    public class BehaviorSettings
    {
        public bool Figure8Swimming { get; set; } = true;
        public bool DepthDiving { get; set; } = true;
        public bool EdgeExploration { get; set; } = true;
        public bool ZigzagSwimming { get; set; } = true;
        public bool CircularOrbiting { get; set; } = true;
    }
    
    // Shark personality traits
    public enum SharkPersonality
    {
        AggressiveHunter,    // Fast, attacks frequently
        CuriousExplorer,     // Investigates everything
        LazyDrifter,         // Slow, prefers rest
        SocialSchooler       // Follows other sharks
    }
    
    // Shark states for behavior tracking
    public enum SharkState 
    { 
        Patrol, Stalking, Alert, Circling, Hunt, Seeking, Attacking, Eating 
    }
    
    // Enhanced idle behavior system
    public enum IdleBehavior 
    { 
        SimplePacrol, DepthDiving, ZigzagSwim, Figure8Swim, RestPause, SlowDrift, EdgeExplore, RandomExplore, CircleArea 
    }
    
    // Individual shark instance
    public class SharkInstance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SharkPersonality Personality { get; set; }
        public Point Position { get; set; }
        public Point PreviousPosition { get; set; }
        public double SwimAngle { get; set; } = 0;
        public bool FacingRight { get; set; } = true;
        public double EnergyLevel { get; set; } = 1.0;
        public double CurrentSpeed { get; set; } = 0.02;
        public double TargetSpeed { get; set; } = 0.02;
        public double PatrolDirection { get; set; } = 1;
        public double PatrolY { get; set; }
        public DateTime LastBehaviorChange { get; set; } = DateTime.Now;
        public IdleBehavior CurrentIdleBehavior { get; set; } = IdleBehavior.SimplePacrol;
        public Point IdleTarget { get; set; } = new Point(0, 0);
        public double IdleBehaviorTimer { get; set; } = 0;
        public bool IsResting { get; set; } = false;
        public DateTime RestStartTime { get; set; } = DateTime.Now;
        public SharkState CurrentState { get; set; } = SharkState.Patrol;
        public int CurrentSwimFrame { get; set; } = 0;
        public double PersonalityMultiplier { get; set; } = 1.0;
        
        // UI components for this shark
        public Image? SharkFinImage { get; set; }
        public Image? BiteImage { get; set; }
        
        // Schooling behavior
        public SharkInstance? LeaderShark { get; set; }
        public List<SharkInstance> FollowerSharks { get; set; } = new List<SharkInstance>();
        public double SchoolingDistance { get; set; } = 80;
        
        public SharkInstance(SharkPersonality personality)
        {
            Personality = personality;
            SetPersonalityTraits();
        }
        
        private void SetPersonalityTraits()
        {
            switch (Personality)
            {
                case SharkPersonality.AggressiveHunter:
                    PersonalityMultiplier = 1.5; // Faster movement
                    EnergyLevel = 0.9; // High energy
                    break;
                case SharkPersonality.CuriousExplorer:
                    PersonalityMultiplier = 1.2; // Slightly faster
                    EnergyLevel = 0.8; // Good energy
                    break;
                case SharkPersonality.LazyDrifter:
                    PersonalityMultiplier = 0.6; // Slower movement
                    EnergyLevel = 0.4; // Lower energy
                    break;
                case SharkPersonality.SocialSchooler:
                    PersonalityMultiplier = 1.0; // Normal speed
                    EnergyLevel = 0.7; // Moderate energy
                    SchoolingDistance = 60; // Closer schooling
                    break;
            }
        }
    }
    
    public partial class MainWindow : Window
    {
        // Win32 API imports for mouse hooks
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr hCursor);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Class members
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelMouseProc _proc = HookCallback;
        private static MainWindow _instance;
        
        private DispatcherTimer _idleTimer;
        private DispatcherTimer _animationTimer;
        private DateTime _lastMouseActivity;
        
        private Canvas _sharkCanvas;
        private Image _sharkFinImage;
        private Image _biteImage;
        
        private Point _currentMousePos;
        private Point _sharkPos;
        private Point _previousSharkPos;
        private Point _lastClickPos;
        private double _swimAngle = 0;
        private bool _facingRight = true;
        private const double DIRECTION_THRESHOLD = 1.0; // Minimum movement to trigger direction change
        
        // Speed constants
        private const double PATROL_SPEED = 0.02;
        private const double ALERT_SPEED = 0.15;
        private const double HUNT_SPEED = 0.25;
        private const double ATTACK_SPEED = 0.3;
        
        // Smooth transition variables
        private double _currentSpeed = 0.02; // Start at patrol speed
        private double _targetSpeed = 0.02;
        private const double SPEED_TRANSITION_RATE = 0.08; // How fast speed changes
        
        // Patrol mode variables
        private double _patrolDirection = 1; // 1 for right, -1 for left
        private double _patrolY;
        private DateTime _lastClickTime;
        private DateTime _seekStartTime;
        private bool _wasCirclingWhenMouseMoved = false;
        
        // Detection ranges
        private const double DETECTION_RANGE = 150.0; // Range for patrol mouse detection
        private const double STALKING_RANGE = 200.0; // Range for stalking detection
        private const double MOUSE_NEARBY_RANGE = 100.0; // Range to consider mouse "nearby" at click point
        private const double SEEK_DURATION = 2.0; // Seconds to seek before returning to patrol
        private const double STALKING_DURATION = 4.0; // Seconds to stalk before seeking
        
        // Behavioral tracking
        private int _escapeCount = 0; // Track how many times mouse has escaped
        private DateTime _lastEscapeTime;
        private DateTime _stalkingStartTime;
        private DateTime _attackStartTime;
        private bool _isFrustrated = false;
        private bool _isTired = false;
        private const int FRUSTRATION_THRESHOLD = 3; // Escapes needed to trigger frustration
        private const double ATTACK_TIMEOUT = 3.5; // Seconds before attack is considered a miss
        
        private SharkState _currentState = SharkState.Patrol;
        private IdleBehavior _currentIdleBehavior = IdleBehavior.SimplePacrol;
        private DateTime _lastIdleBehaviorChange = DateTime.Now;
        private Random _behaviorRandom = new Random();
        private Point _idleTarget = new Point(0, 0);
        private double _idleBehaviorTimer = 0;
        private double _energyLevel = 1.0; // 0.0 to 1.0, affects speed and activity
        private DateTime _lastEnergyChange = DateTime.Now;
        private bool _isResting = false;
        private DateTime _restStartTime = DateTime.Now;
        
        // Idle behavior constants
        private const double BEHAVIOR_CHANGE_MIN_INTERVAL = 3.0; // Minimum seconds between behavior changes
        private const double BEHAVIOR_CHANGE_MAX_INTERVAL = 12.0; // Maximum seconds between behavior changes
        private const double ENERGY_DECAY_RATE = 0.1; // How fast energy decreases
        private const double ENERGY_RECOVERY_RATE = 0.2; // How fast energy recovers during rest
        private const double REST_PROBABILITY = 0.15; // 15% chance to rest when energy is low
        private const double DEEP_SLEEP_THRESHOLD = 300.0; // 5 minutes idle = deep sleep mode
        
        private bool _mouseHidden = false;
        private IntPtr _originalCursor;
        
        // Sprite animation system
        private List<BitmapImage> _swimSprites;
        private List<BitmapImage> _biteSprites;
        private int _currentSwimFrame = 0;
        private int _currentBiteFrame = 0;
        private int _frameCounter = 0;
        private bool _biteAnimationPlaying = false;
        
        // System tray components
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private SharkSettings _settings;
        private string _settingsPath;

        public MainWindow()
        {
            _instance = this;
            SetupWindow();
            SetupSharkGraphics();
            SetupTimers();
            SetupMouseHook();
            SetupSystemTray();
            LoadSettings();
        }

        private void SetupWindow()
        {
            // Make window transparent and click-through
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            
            // Cover entire virtual screen (all monitors)
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            
            // Don't use Maximized state for multi-monitor support
            WindowState = WindowState.Normal;
        }

        private void SetupSharkGraphics()
        {
            _sharkCanvas = new Canvas();
            Content = _sharkCanvas;
            
            // Load sprite images
            LoadSprites();
            
            // Create shark fin image
            _sharkFinImage = new Image
            {
                Width = 32,
                Height = 24,
                Source = _swimSprites[0]
            };
            
            // Create bite effect image (initially hidden)
            _biteImage = new Image
            {
                Width = 64,
                Height = 64,
                Source = _biteSprites[0],
                Visibility = Visibility.Hidden
            };
            
            _sharkCanvas.Children.Add(_sharkFinImage);
            _sharkCanvas.Children.Add(_biteImage);
            
            // Initial position
            _sharkPos = new Point(100, 100);
            UpdateSharkPosition();
        }

        private void SetupTimers()
        {
            // Idle detection timer
            _idleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            _idleTimer.Tick += IdleTimer_Tick;
            _idleTimer.Start();
            
            // Animation timer for smooth movement
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();
            
            _lastMouseActivity = DateTime.Now;
        }

        private void SetupMouseHook()
        {
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                _instance?.OnMouseActivity(wParam);
            }
            return CallNextHookEx(_instance._hookID, nCode, wParam, lParam);
        }

        private void OnMouseActivity(IntPtr wParam)
        {
            _lastMouseActivity = DateTime.Now;
            
            // Get current mouse position
            GetCursorPos(out POINT point);
            _currentMousePos = new Point(point.x, point.y);
            
            // Handle click detection
            if (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN)
            {
                _lastClickPos = _currentMousePos;
                _lastClickTime = DateTime.Now;
                
                // Transition to Alert state on click
                if (_currentState == SharkState.Patrol)
                {
                    _currentState = SharkState.Alert;
                }
            }
            
            // Handle mouse movement during circling
            if (wParam == (IntPtr)WM_MOUSEMOVE && _currentState == SharkState.Circling)
            {
                _wasCirclingWhenMouseMoved = true;
                _currentState = SharkState.Hunt;
            }
            
            // If mouse was hidden, restore it
            if (_mouseHidden)
            {
                RestoreMouse();
            }
            
            // Reset states if attacking or eating
            if (_currentState == SharkState.Attacking || _currentState == SharkState.Eating)
            {
                _currentState = SharkState.Patrol;
                _biteImage.Visibility = Visibility.Hidden;
                _biteAnimationPlaying = false;
                _sharkFinImage.Visibility = Visibility.Visible;
                SpawnAtRandomPosition();
            }
        }

        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            var timeSinceActivity = DateTime.Now - _lastMouseActivity;
            
            switch (_currentState)
            {
                case SharkState.Alert:
                    // Use shark center point for consistent detection
                    var sharkCenterX = _sharkPos.X + 16;
                    var sharkCenterY = _sharkPos.Y + 12;
                    var distanceToClick = Math.Sqrt(Math.Pow(sharkCenterX - _lastClickPos.X, 2) + 
                                                  Math.Pow(sharkCenterY - _lastClickPos.Y, 2));
                    if (distanceToClick < 30)
                    {
                        // Check if mouse is at the click location for direct attack
                        var mouseDistanceFromClick = Math.Sqrt(Math.Pow(_currentMousePos.X - _lastClickPos.X, 2) + 
                                                              Math.Pow(_currentMousePos.Y - _lastClickPos.Y, 2));
                        
                        if (mouseDistanceFromClick < 30)
                        {
                            // Mouse is at click location - DIRECT ATTACK (no circling!)
                            StartAttack();
                        }
                        else
                        {
                            // Mouse is not at click location - resume patrol from this position
                            _patrolY = _sharkPos.Y;
                            _currentState = SharkState.Patrol;
                        }
                    }
                    break;
                    
                case SharkState.Circling:
                    var timeSinceClick = DateTime.Now - _lastClickTime;
                    if (timeSinceClick.TotalSeconds >= 3)
                    {
                        StartAttack();
                    }
                    break;
                    
                case SharkState.Hunt:
                    // Use shark center point for consistent detection
                    var huntSharkCenterX = _sharkPos.X + 16;
                    var huntSharkCenterY = _sharkPos.Y + 12;
                    var distanceToMouse = Math.Sqrt(Math.Pow(huntSharkCenterX - _currentMousePos.X, 2) + 
                                                   Math.Pow(huntSharkCenterY - _currentMousePos.Y, 2));
                    if (distanceToMouse < 50)
                    {
                        StartAttack();
                    }
                    break;
                    
                case SharkState.Stalking:
                    var stalkingDuration = DateTime.Now - _stalkingStartTime;
                    // Use shark center point for consistent detection
                    var stalkingSharkCenterX = _sharkPos.X + 16;
                    var stalkingSharkCenterY = _sharkPos.Y + 12;
                    var stalkingMouseDistance = Math.Sqrt(Math.Pow(stalkingSharkCenterX - _currentMousePos.X, 2) + 
                                                         Math.Pow(stalkingSharkCenterY - _currentMousePos.Y, 2));
                    
                    if (stalkingMouseDistance < DETECTION_RANGE)
                    {
                        // Mouse got too close - transition to Seeking
                        _currentState = SharkState.Seeking;
                        _seekStartTime = DateTime.Now;
                    }
                    else if (stalkingDuration.TotalSeconds >= STALKING_DURATION)
                    {
                        // Stalking timeout - transition to Seeking for aggressive pursuit
                        _currentState = SharkState.Seeking;
                        _seekStartTime = DateTime.Now;
                    }
                    break;
                    
                case SharkState.Seeking:
                    // Check if close enough to attack during seeking
                    var seekingSharkCenterX = _sharkPos.X + 16;
                    var seekingSharkCenterY = _sharkPos.Y + 12;
                    var seekingDistanceToMouse = Math.Sqrt(Math.Pow(seekingSharkCenterX - _currentMousePos.X, 2) + 
                                                           Math.Pow(seekingSharkCenterY - _currentMousePos.Y, 2));
                    
                    if (seekingDistanceToMouse < 60)
                    {
                        // Close enough to attack during seeking
                        StartAttack();
                        break;
                    }
                    
                    var seekDuration = DateTime.Now - _seekStartTime;
                    if (seekDuration.TotalSeconds >= SEEK_DURATION)
                    {
                        // Return to patrol after seeking for 2 seconds
                        // Update patrol Y to current position to prevent warping
                        _patrolY = _sharkPos.Y;
                        _currentState = SharkState.Patrol;
                    }
                    break;
                    
                case SharkState.Attacking:
                    var attackDuration = DateTime.Now - _attackStartTime;
                    if (attackDuration.TotalSeconds >= ATTACK_TIMEOUT)
                    {
                        // Attack timed out - shark missed and becomes tired
                        _escapeCount++;
                        _isTired = true;
                        _lastEscapeTime = DateTime.Now;
                        
                        // Check if frustrated
                        if (_escapeCount >= FRUSTRATION_THRESHOLD)
                        {
                            _isFrustrated = true;
                        }
                        
                        // Transition to patrol when tired
                        _patrolY = _sharkPos.Y;
                        _currentState = SharkState.Patrol;
                        
                        // Reset shark size
                        ResetSharkSize();
                    }
                    break;
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            UpdateSpeedTransitions();
            UpdateEnergySystem();
            UpdateIdleBehaviors();
            UpdateSharkBehavior();
            UpdateSharkPosition();
            UpdateSpriteAnimations();
        }
        
        private void UpdateSpeedTransitions()
        {
            // Smoothly interpolate current speed toward target speed
            if (Math.Abs(_currentSpeed - _targetSpeed) > 0.001)
            {
                var speedDifference = _targetSpeed - _currentSpeed;
                _currentSpeed += speedDifference * SPEED_TRANSITION_RATE;
                
                // Snap to target if very close
                if (Math.Abs(speedDifference) < 0.001)
                {
                    _currentSpeed = _targetSpeed;
                }
            }
        }
        
        private void UpdateEnergySystem()
        {
            var timeSinceActivity = DateTime.Now - _lastMouseActivity;
            var deltaTime = (DateTime.Now - _lastEnergyChange).TotalSeconds;
            _lastEnergyChange = DateTime.Now;
            
            if (_isResting)
            {
                // Recover energy during rest
                _energyLevel = Math.Min(1.0, _energyLevel + ENERGY_RECOVERY_RATE * deltaTime);
                
                // Check if rest period is over
                var restDuration = DateTime.Now - _restStartTime;
                if (restDuration.TotalSeconds >= 2.0 || _energyLevel >= 0.8)
                {
                    _isResting = false;
                }
            }
            else
            {
                // Gradual energy decay during activity
                _energyLevel = Math.Max(0.0, _energyLevel - ENERGY_DECAY_RATE * deltaTime);
                
                // Check if shark should rest
                if (_energyLevel < 0.3 && _behaviorRandom.NextDouble() < REST_PROBABILITY)
                {
                    _isResting = true;
                    _restStartTime = DateTime.Now;
                    _currentIdleBehavior = IdleBehavior.RestPause;
                }
            }
            
            // Deep sleep mode if idle for too long
            if (timeSinceActivity.TotalSeconds > DEEP_SLEEP_THRESHOLD)
            {
                _energyLevel = Math.Max(0.1, _energyLevel); // Minimum energy in deep sleep
                _currentIdleBehavior = IdleBehavior.SlowDrift;
            }
        }
        
        private void UpdateIdleBehaviors()
        {
            // Only update idle behaviors during patrol state
            if (_currentState != SharkState.Patrol) return;
            
            var timeSinceBehaviorChange = DateTime.Now - _lastIdleBehaviorChange;
            _idleBehaviorTimer += 0.016; // 16ms per frame
            
            // Check if it's time to change behavior
            var behaviorDuration = BEHAVIOR_CHANGE_MIN_INTERVAL + 
                                  _behaviorRandom.NextDouble() * (BEHAVIOR_CHANGE_MAX_INTERVAL - BEHAVIOR_CHANGE_MIN_INTERVAL);
            
            if (timeSinceBehaviorChange.TotalSeconds >= behaviorDuration)
            {
                ChangeIdleBehavior();
            }
            
            // Execute current idle behavior
            ExecuteCurrentIdleBehavior();
        }
        
        private void ChangeIdleBehavior()
        {
            _lastIdleBehaviorChange = DateTime.Now;
            _idleBehaviorTimer = 0;
            
            // Select new behavior based on energy level
            var availableBehaviors = new List<IdleBehavior>();
            
            if (_energyLevel > 0.7)
            {
                // High energy - more active behaviors
                availableBehaviors.AddRange(new[] { 
                    IdleBehavior.ZigzagSwim, IdleBehavior.Figure8Swim, 
                    IdleBehavior.EdgeExplore, IdleBehavior.RandomExplore, 
                    IdleBehavior.CircleArea 
                });
            }
            else if (_energyLevel > 0.4)
            {
                // Medium energy - mixed behaviors
                availableBehaviors.AddRange(new[] { 
                    IdleBehavior.SimplePacrol, IdleBehavior.DepthDiving, 
                    IdleBehavior.RandomExplore 
                });
            }
            else
            {
                // Low energy - passive behaviors
                availableBehaviors.AddRange(new[] { 
                    IdleBehavior.RestPause, IdleBehavior.SlowDrift, 
                    IdleBehavior.SimplePacrol 
                });
            }
            
            // Don't repeat the same behavior immediately
            availableBehaviors.Remove(_currentIdleBehavior);
            
            if (availableBehaviors.Count > 0)
            {
                _currentIdleBehavior = availableBehaviors[_behaviorRandom.Next(availableBehaviors.Count)];
                SetupNewIdleBehavior();
            }
        }
        
        private void SetupNewIdleBehavior()
        {
            switch (_currentIdleBehavior)
            {
                case IdleBehavior.EdgeExplore:
                    // Pick a random screen edge to explore
                    var edges = new[] { "left", "right", "top", "bottom" };
                    var edge = edges[_behaviorRandom.Next(edges.Length)];
                    switch (edge)
                    {
                        case "left":
                            _idleTarget = new Point(50, _behaviorRandom.NextDouble() * SystemParameters.VirtualScreenHeight);
                            break;
                        case "right":
                            _idleTarget = new Point(SystemParameters.VirtualScreenWidth - 50, _behaviorRandom.NextDouble() * SystemParameters.VirtualScreenHeight);
                            break;
                        case "top":
                            _idleTarget = new Point(_behaviorRandom.NextDouble() * SystemParameters.VirtualScreenWidth, 50);
                            break;
                        case "bottom":
                            _idleTarget = new Point(_behaviorRandom.NextDouble() * SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight - 50);
                            break;
                    }
                    break;
                    
                case IdleBehavior.RandomExplore:
                    // Pick a random point on screen to investigate
                    _idleTarget = new Point(
                        _behaviorRandom.NextDouble() * SystemParameters.VirtualScreenWidth,
                        _behaviorRandom.NextDouble() * SystemParameters.VirtualScreenHeight
                    );
                    break;
                    
                case IdleBehavior.CircleArea:
                    // Pick a point to circle around
                    _idleTarget = new Point(
                        200 + _behaviorRandom.NextDouble() * (SystemParameters.VirtualScreenWidth - 400),
                        200 + _behaviorRandom.NextDouble() * (SystemParameters.VirtualScreenHeight - 400)
                    );
                    break;
            }
        }
        
        private void ExecuteCurrentIdleBehavior()
        {
            switch (_currentIdleBehavior)
            {
                case IdleBehavior.SimplePacrol:
                    // Default patrol behavior - already handled in UpdateSharkBehavior
                    break;
                    
                case IdleBehavior.Figure8Swim:
                    ExecuteFigure8Swimming();
                    break;
                    
                case IdleBehavior.DepthDiving:
                    ExecuteDepthDiving();
                    break;
                    
                case IdleBehavior.ZigzagSwim:
                    ExecuteZigzagSwimming();
                    break;
                    
                case IdleBehavior.EdgeExplore:
                    ExecuteEdgeExploration();
                    break;
                    
                case IdleBehavior.RandomExplore:
                    ExecuteRandomExploration();
                    break;
                    
                case IdleBehavior.CircleArea:
                    ExecuteCircularOrbiting();
                    break;
                    
                case IdleBehavior.RestPause:
                    ExecuteRestPause();
                    break;
                    
                case IdleBehavior.SlowDrift:
                    ExecuteSlowDrift();
                    break;
            }
        }
        
        private void ExecuteFigure8Swimming()
        {
            // Parametric figure-8 pattern: X = radiusX * sin(t), Y = radiusY * sin(2t)
            var centerX = SystemParameters.VirtualScreenWidth / 2;
            var centerY = SystemParameters.VirtualScreenHeight / 2;
            var radiusX = 200;
            var radiusY = 100;
            
            _sharkPos.X = centerX + radiusX * Math.Sin(_idleBehaviorTimer);
            _sharkPos.Y = centerY + radiusY * Math.Sin(2 * _idleBehaviorTimer);
        }
        
        private void ExecuteDepthDiving()
        {
            // Vertical sine wave movement with occasional deep dives
            var amplitude = 50 + 150 * _energyLevel; // Larger dives when more energetic
            var frequency = 0.02 + 0.03 * _energyLevel;
            
            // Horizontal movement
            _sharkPos.X += _patrolDirection * _currentSpeed * 30;
            
            // Vertical oscillation with random deep dives
            _sharkPos.Y = _patrolY + amplitude * Math.Sin(_idleBehaviorTimer * frequency);
            
            // Occasional deep dive
            if (_behaviorRandom.NextDouble() < 0.002) // 0.2% chance per frame
            {
                _patrolY = Math.Max(100, SystemParameters.VirtualScreenHeight * 0.8);
            }
        }
        
        private void ExecuteZigzagSwimming()
        {
            // Diagonal movement with periodic direction changes
            var zigzagAmplitude = 80;
            var zigzagFrequency = 0.1;
            
            // Forward movement with zigzag pattern
            _sharkPos.X += _patrolDirection * _currentSpeed * 40;
            _sharkPos.Y += zigzagAmplitude * Math.Sin(_idleBehaviorTimer * zigzagFrequency) * 0.5;
            
            // Random direction changes
            if (_behaviorRandom.NextDouble() < 0.01) // 1% chance per frame
            {
                _patrolDirection *= -1;
            }
        }
        
        private void ExecuteEdgeExploration()
        {
            // Move toward the selected edge target
            var deltaX = _idleTarget.X - _sharkPos.X;
            var deltaY = _idleTarget.Y - _sharkPos.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            
            if (distance > 20)
            {
                // Move toward target
                var speed = _currentSpeed * 60;
                _sharkPos.X += (deltaX / distance) * speed;
                _sharkPos.Y += (deltaY / distance) * speed;
            }
            else
            {
                // Reached edge - pause briefly
                if (_idleBehaviorTimer > 2.0)
                {
                    ChangeIdleBehavior(); // Move to next behavior
                }
            }
        }
        
        private void ExecuteRandomExploration()
        {
            // Move toward random exploration target
            var deltaX = _idleTarget.X - _sharkPos.X;
            var deltaY = _idleTarget.Y - _sharkPos.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            
            if (distance > 30)
            {
                // Move toward target with some wandering
                var speed = _currentSpeed * 50;
                _sharkPos.X += (deltaX / distance) * speed + Math.Sin(_idleBehaviorTimer) * 2;
                _sharkPos.Y += (deltaY / distance) * speed + Math.Cos(_idleBehaviorTimer) * 2;
            }
            else
            {
                // Reached target - select new one
                SetupNewIdleBehavior();
            }
        }
        
        private void ExecuteCircularOrbiting()
        {
            // Circle around the selected target point
            var radius = 150;
            var orbitSpeed = 0.05 + 0.05 * _energyLevel;
            
            _sharkPos.X = _idleTarget.X + radius * Math.Cos(_idleBehaviorTimer * orbitSpeed);
            _sharkPos.Y = _idleTarget.Y + radius * Math.Sin(_idleBehaviorTimer * orbitSpeed);
        }
        
        private void ExecuteRestPause()
        {
            // Minimal movement - gentle floating
            _sharkPos.Y += Math.Sin(_idleBehaviorTimer * 0.02) * 2; // Very gentle bobbing
            
            // Very slow forward drift
            if (!_isResting)
            {
                _sharkPos.X += _patrolDirection * 0.3;
            }
        }
        
        private void ExecuteSlowDrift()
        {
            // Extremely slow movement for deep sleep mode
            var driftSpeed = 0.5;
            _sharkPos.X += _patrolDirection * driftSpeed;
            _sharkPos.Y += Math.Sin(_idleBehaviorTimer * 0.01) * 1; // Very slow vertical drift
        }

        private void UpdateSharkBehavior()
        {
            // Store previous position for direction tracking
            _previousSharkPos = _sharkPos;
            
            switch (_currentState)
            {
                case SharkState.Patrol:
                    // Set target speed for patrol
                    _targetSpeed = PATROL_SPEED;
                    
                    // Check for mouse detection during patrol using shark center point
                    var patrolSharkCenterX = _sharkPos.X + 16;
                    var patrolSharkCenterY = _sharkPos.Y + 12;
                    var patrolMouseDistance = Math.Sqrt(Math.Pow(patrolSharkCenterX - _currentMousePos.X, 2) + 
                                                       Math.Pow(patrolSharkCenterY - _currentMousePos.Y, 2));
                    
                    if (patrolMouseDistance < DETECTION_RANGE)
                    {
                        // Mouse detected - transition to Seeking
                        _currentState = SharkState.Seeking;
                        _seekStartTime = DateTime.Now;
                        _targetSpeed = ALERT_SPEED; // Set target for smooth transition
                        break;
                    }
                    else if (patrolMouseDistance < STALKING_RANGE)
                    {
                        // Mouse in stalking range - transition to Stalking
                        _currentState = SharkState.Stalking;
                        _stalkingStartTime = DateTime.Now;
                        _targetSpeed = PATROL_SPEED; // Stalking uses slow speed
                        break;
                    }
                    
                    // Slow left-to-right movement across screen using smooth speed
                    _sharkPos.X += _patrolDirection * _currentSpeed * 50;
                    _sharkPos.Y = _patrolY + Math.Sin(_swimAngle) * 10;
                    _swimAngle += 0.05;
                    
                    // Wrap around screen edges smoothly
                    if (_sharkPos.X > SystemParameters.VirtualScreenWidth + 32)
                    {
                        _sharkPos.X = -32; // Appear on left side
                        // Keep same direction - no backwards swimming!
                    }
                    else if (_sharkPos.X < -32)
                    {
                        _sharkPos.X = SystemParameters.VirtualScreenWidth + 32; // Appear on right side
                    }
                    break;
                    
                case SharkState.Stalking:
                    // Slow, stealthy following at distance
                    var stalkTargetX = _currentMousePos.X - 15;
                    var stalkTargetY = _currentMousePos.Y + 60; // Stay further away
                    
                    var stalkDeltaX = stalkTargetX - _sharkPos.X;
                    var stalkDeltaY = stalkTargetY - _sharkPos.Y;
                    var stalkDistance = Math.Sqrt(stalkDeltaX * stalkDeltaX + stalkDeltaY * stalkDeltaY);
                    
                    // Very slow stalking movement with subtle oscillation
                    if (stalkDistance > 10)
                    {
                        _sharkPos.X += (stalkDeltaX / stalkDistance) * PATROL_SPEED * 30;
                        _sharkPos.Y += (stalkDeltaY / stalkDistance) * PATROL_SPEED * 30;
                    }
                    
                    // Add subtle stalking oscillation
                    _swimAngle += 0.03;
                    _sharkPos.Y += Math.Sin(_swimAngle) * 5;
                    break;
                    
                case SharkState.Alert:
                    // Set target speed for alert state
                    _targetSpeed = ALERT_SPEED;
                    
                    // Medium speed movement directly to click position (no offset)
                    var alertTargetX = _lastClickPos.X;
                    var alertTargetY = _lastClickPos.Y;
                    
                    var alertDeltaX = alertTargetX - _sharkPos.X;
                    var alertDeltaY = alertTargetY - _sharkPos.Y;
                    var alertDistance = Math.Sqrt(alertDeltaX * alertDeltaX + alertDeltaY * alertDeltaY);
                    
                    if (alertDistance > 5)
                    {
                        _sharkPos.X += (alertDeltaX / alertDistance) * _currentSpeed * 50;
                        _sharkPos.Y += (alertDeltaY / alertDistance) * _currentSpeed * 50;
                    }
                    break;
                    
                case SharkState.Circling:
                    // Circling motion around click position
                    _swimAngle += 0.08;
                    var circleRadius = 60;
                    _sharkPos.X = _lastClickPos.X + Math.Cos(_swimAngle) * circleRadius - 15;
                    _sharkPos.Y = _lastClickPos.Y + Math.Sin(_swimAngle) * circleRadius + 30;
                    break;
                    
                case SharkState.Hunt:
                    // Fast pursuit of mouse - almost keeping up
                    var huntTargetX = _currentMousePos.X - 15;
                    var huntTargetY = _currentMousePos.Y + 30;
                    
                    var huntDeltaX = huntTargetX - _sharkPos.X;
                    var huntDeltaY = huntTargetY - _sharkPos.Y;
                    var huntDistance = Math.Sqrt(huntDeltaX * huntDeltaX + huntDeltaY * huntDeltaY);
                    
                    if (huntDistance > 5)
                    {
                        _sharkPos.X += (huntDeltaX / huntDistance) * HUNT_SPEED * 50;
                        _sharkPos.Y += (huntDeltaY / huntDistance) * HUNT_SPEED * 50;
                    }
                    
                    // Add some hunting oscillation
                    _swimAngle += 0.3;
                    _sharkPos.Y += Math.Sin(_swimAngle) * 3;
                    break;
                    
                case SharkState.Seeking:
                    // Medium speed pursuit during seek mode
                    var seekTargetX = _currentMousePos.X - 15;
                    var seekTargetY = _currentMousePos.Y + 30;
                    
                    var seekDeltaX = seekTargetX - _sharkPos.X;
                    var seekDeltaY = seekTargetY - _sharkPos.Y;
                    var seekDistance = Math.Sqrt(seekDeltaX * seekDeltaX + seekDeltaY * seekDeltaY);
                    
                    if (seekDistance > 5)
                    {
                        _sharkPos.X += (seekDeltaX / seekDistance) * ALERT_SPEED * 50;
                        _sharkPos.Y += (seekDeltaY / seekDistance) * ALERT_SPEED * 50;
                    }
                    
                    // Add slight oscillation for seeking behavior
                    _swimAngle += 0.2;
                    _sharkPos.Y += Math.Sin(_swimAngle) * 2;
                    break;
                    
                case SharkState.Attacking:
                    // Quick lunge toward mouse - move shark center toward mouse position
                    var sharkCenterX = _sharkPos.X + 16;
                    var sharkCenterY = _sharkPos.Y + 12;
                    
                    var deltaX = _currentMousePos.X - sharkCenterX;
                    var deltaY = _currentMousePos.Y - sharkCenterY;
                    
                    _sharkPos.X += deltaX * ATTACK_SPEED;
                    _sharkPos.Y += deltaY * ATTACK_SPEED;
                    
                    // Check distance from shark center to mouse for bite detection
                    var newSharkCenterX = _sharkPos.X + 16;
                    var newSharkCenterY = _sharkPos.Y + 12;
                    var distance = Math.Sqrt(Math.Pow(newSharkCenterX - _currentMousePos.X, 2) + 
                                           Math.Pow(newSharkCenterY - _currentMousePos.Y, 2));
                    if (distance < 40)
                    {
                        ExecuteBite();
                    }
                    break;
            }
        }

        private void StartAttack()
        {
            _currentState = SharkState.Attacking;
            _attackStartTime = DateTime.Now; // Track when attack started
            
            // Animate fin growing larger for attack while preserving flip
            var transformGroup = _sharkFinImage.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                _sharkFinImage.RenderTransform = transformGroup;
            }
            
            transformGroup.Children.Clear();
            var flipTransform = new ScaleTransform();
            flipTransform.ScaleX = _facingRight ? 1 : -1;
            flipTransform.ScaleY = 1;
            transformGroup.Children.Add(flipTransform);
            
            var attackScaleTransform = new ScaleTransform(1, 1);
            transformGroup.Children.Add(attackScaleTransform);
            
            _sharkFinImage.RenderTransformOrigin = new Point(0.5, 0.5);
            
            var scaleAnimation = new DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(300));
            attackScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            attackScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }
        
        private void ResetSharkSize()
        {
            var transformGroup = _sharkFinImage.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                _sharkFinImage.RenderTransform = transformGroup;
            }
            
            transformGroup.Children.Clear();
            var flipTransform = new ScaleTransform();
            flipTransform.ScaleX = _facingRight ? 1 : -1;
            flipTransform.ScaleY = 1;
            transformGroup.Children.Add(flipTransform);
            
            _sharkFinImage.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void ExecuteBite()
        {
            _currentState = SharkState.Eating;
            _mouseHidden = true;
            _biteAnimationPlaying = true;
            _currentBiteFrame = 0;
            
            // Hide mouse cursor
            SetCursor(IntPtr.Zero);
            
            // Hide shark fin during bite animation
            _sharkFinImage.Visibility = Visibility.Hidden;
            
            // Show bite effect and start animation
            _biteImage.Visibility = Visibility.Visible;
            // Convert absolute screen coordinates to virtual screen relative coordinates
            var biteCanvasX = _currentMousePos.X - 32 - SystemParameters.VirtualScreenLeft;
            var biteCanvasY = _currentMousePos.Y - 32 - SystemParameters.VirtualScreenTop;
            Canvas.SetLeft(_biteImage, biteCanvasX);
            Canvas.SetTop(_biteImage, biteCanvasY);
            
            // Start bite animation sequence
            var fadeAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1));
            fadeAnimation.Completed += (s, e) => 
            {
                _biteImage.Visibility = Visibility.Hidden;
                _biteImage.Opacity = 1;
                _biteAnimationPlaying = false;
            };
            _biteImage.BeginAnimation(OpacityProperty, fadeAnimation);
            
            // Reset shark size while preserving flip
            var transformGroup = _sharkFinImage.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                _sharkFinImage.RenderTransform = transformGroup;
            }
            
            transformGroup.Children.Clear();
            var flipTransform = new ScaleTransform();
            flipTransform.ScaleX = _facingRight ? 1 : -1;
            flipTransform.ScaleY = 1;
            transformGroup.Children.Add(flipTransform);
            
            var resetScaleTransform = new ScaleTransform(1.5, 1.5);
            transformGroup.Children.Add(resetScaleTransform);
            
            _sharkFinImage.RenderTransformOrigin = new Point(0.5, 0.5);
            
            var resetScale = new DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500));
            resetScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, resetScale);
            resetScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, resetScale);
        }

        private void RestoreMouse()
        {
            _mouseHidden = false;
            
            // Restore cursor (Windows will handle this automatically)
            // The cursor becomes visible again on next movement
        }
        
        private void SpawnAtRandomPosition()
        {
            var random = new Random();
            _sharkPos = new Point(
                random.NextDouble() * SystemParameters.VirtualScreenWidth,
                random.NextDouble() * SystemParameters.VirtualScreenHeight
            );
            
            // Set patrol Y position for horizontal movement
            _patrolY = _sharkPos.Y;
        }

        private void UpdateSharkDirection()
        {
            var deltaX = _sharkPos.X - _previousSharkPos.X;
            
            if (Math.Abs(deltaX) > DIRECTION_THRESHOLD)
            {
                var shouldFaceRight = deltaX > 0;
                
                if (shouldFaceRight != _facingRight)
                {
                    _facingRight = shouldFaceRight;
                    ApplySpriteFlip();
                }
            }
        }
        
        private void ApplySpriteFlip()
        {
            var transformGroup = _sharkFinImage.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                _sharkFinImage.RenderTransform = transformGroup;
            }
            
            transformGroup.Children.Clear();
            
            var scaleTransform = new ScaleTransform();
            scaleTransform.ScaleX = _facingRight ? 1 : -1;
            scaleTransform.ScaleY = 1;
            
            _sharkFinImage.RenderTransformOrigin = new Point(0.5, 0.5);
            transformGroup.Children.Add(scaleTransform);
            
            var biteTransformGroup = _biteImage.RenderTransform as TransformGroup;
            if (biteTransformGroup == null)
            {
                biteTransformGroup = new TransformGroup();
                _biteImage.RenderTransform = biteTransformGroup;
            }
            
            biteTransformGroup.Children.Clear();
            var biteScaleTransform = new ScaleTransform();
            biteScaleTransform.ScaleX = _facingRight ? 1 : -1;
            biteScaleTransform.ScaleY = 1;
            
            _biteImage.RenderTransformOrigin = new Point(0.5, 0.5);
            biteTransformGroup.Children.Add(biteScaleTransform);
        }

        private void UpdateSharkPosition()
        {
            // Update direction based on movement
            UpdateSharkDirection();
            
            // Convert absolute screen coordinates to virtual screen relative coordinates
            var canvasX = _sharkPos.X - SystemParameters.VirtualScreenLeft;
            var canvasY = _sharkPos.Y - SystemParameters.VirtualScreenTop;
            
            Canvas.SetLeft(_sharkFinImage, canvasX);
            Canvas.SetTop(_sharkFinImage, canvasY);
        }
        
        private void UpdateSpriteAnimations()
        {
            _frameCounter++;
            
            // Update swimming animation (8 FPS)
            if (_frameCounter % 8 == 0)
            {
                _currentSwimFrame = (_currentSwimFrame + 1) % _swimSprites.Count;
                _sharkFinImage.Source = _swimSprites[_currentSwimFrame];
            }
            
            // Update bite animation (12 FPS) - only when playing
            if (_biteAnimationPlaying && _frameCounter % 5 == 0)
            {
                _currentBiteFrame++;
                if (_currentBiteFrame < _biteSprites.Count)
                {
                    _biteImage.Source = _biteSprites[_currentBiteFrame];
                }
                else
                {
                    // Animation complete, reset to first frame
                    _currentBiteFrame = 0;
                    _biteImage.Source = _biteSprites[0];
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Make window click-through
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        // Additional Win32 API for click-through
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private void LoadSprites()
        {
            _swimSprites = new List<BitmapImage>();
            _biteSprites = new List<BitmapImage>();
            
            // Load swimming animation sprites
            for (int i = 1; i <= 8; i++)
            {
                var bitmap = LoadBitmapFromFile($"Graphics/shark_fin_swim_{i:D2}.png");
                _swimSprites.Add(bitmap);
            }
            
            // Load bite animation sprites
            for (int i = 1; i <= 12; i++)
            {
                var bitmap = LoadBitmapFromFile($"Graphics/shark_bite_{i:D2}.png");
                _biteSprites.Add(bitmap);
            }
        }
        
        private BitmapImage LoadBitmapFromFile(string relativePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(relativePath, UriKind.Relative);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                // If loading fails, create a fallback placeholder
                Console.WriteLine($"Failed to load sprite: {relativePath} - {ex.Message}");
                return CreateFallbackBitmap();
            }
        }
        
        private BitmapImage CreateFallbackBitmap()
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            
            var drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(70, 70, 70)), null, new Rect(0, 0, 32, 24));
            }
            
            var renderBitmap = new RenderTargetBitmap(32, 24, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);
            
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            
            var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            
            return bitmap;
        }
        


        protected override void OnClosed(EventArgs e)
        {
            // Clean up mouse hook
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
            }
            base.OnClosed(e);
        }

        private void SetupSystemTray()
        {
            // Initialize settings path
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var sharkfinPath = System.IO.Path.Combine(appDataPath, "SharkFinCompanion");
            Directory.CreateDirectory(sharkfinPath);
            _settingsPath = System.IO.Path.Combine(sharkfinPath, "settings.json");
            
            // Create system tray icon
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateSharkIcon(),
                Text = "SharkFin Companion",
                Visible = true
            };
            
            // Create context menu
            CreateContextMenu();
            _notifyIcon.ContextMenuStrip = _contextMenu;
            
            // Handle left-click to show current status
            _notifyIcon.Click += (s, e) =>
            {
                if (((System.Windows.Forms.MouseEventArgs)e).Button == MouseButtons.Left)
                {
                    var statusText = $"Status: {GetCurrentBehaviorName()}\nEnergy: {(_energyLevel * 100):F0}%\nSpeed: {_settings.SpeedMultiplier:F1}x";
                    _notifyIcon.ShowBalloonTip(3000, "SharkFin Companion", statusText, ToolTipIcon.Info);
                }
            };
        }
        
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<SharkSettings>(json) ?? new SharkSettings();
                }
                else
                {
                    _settings = new SharkSettings();
                    SaveSettings();
                }
                
                // Apply loaded settings
                ApplySettings();
            }
            catch
            {
                _settings = new SharkSettings();
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // Settings save failed - continue without saving
            }
        }
        
        private void ApplySettings()
        {
            // Apply speed multiplier and other settings
            // Settings will be checked during behavior updates
        }
        
        private string GetCurrentBehaviorName()
        {
            return _currentIdleBehavior switch
            {
                IdleBehavior.SimplePacrol => "Simple Patrol",
                IdleBehavior.Figure8Swim => "Figure-8 Swimming",
                IdleBehavior.DepthDiving => "Depth Diving",
                IdleBehavior.ZigzagSwim => "Zigzag Swimming",
                IdleBehavior.EdgeExplore => "Edge Exploration",
                IdleBehavior.RandomExplore => "Random Exploration",
                IdleBehavior.CircleArea => "Circular Orbiting",
                IdleBehavior.RestPause => "Resting",
                IdleBehavior.SlowDrift => "Slow Drift",
                _ => "Unknown"
            };
        }
        
        private System.Drawing.Icon CreateSharkIcon()
        {
            // Create a simple shark icon programmatically
            var bitmap = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.Clear(System.Drawing.Color.Transparent);
                
                // Draw simple shark shape
                var brush = new System.Drawing.SolidBrush(System.Drawing.Color.DarkBlue);
                
                // Body
                g.FillEllipse(brush, 2, 6, 12, 4);
                // Fin
                g.FillPolygon(brush, new System.Drawing.Point[] {
                    new System.Drawing.Point(6, 2),
                    new System.Drawing.Point(10, 6),
                    new System.Drawing.Point(8, 6)
                });
                // Tail
                g.FillPolygon(brush, new System.Drawing.Point[] {
                    new System.Drawing.Point(13, 7),
                    new System.Drawing.Point(15, 5),
                    new System.Drawing.Point(15, 9)
                });
                
                brush.Dispose();
            }
            
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }
        
        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            
            // Status header
            var statusItem = new ToolStripLabel("🦈 SharkFin Companion")
            {
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
            };
            _contextMenu.Items.Add(statusItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            // Exit
            var exitItem = new ToolStripMenuItem("❌ Exit");
            exitItem.Click += (s, e) => {
                _notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            };
            _contextMenu.Items.Add(exitItem);
        }
    }

    // App.xaml.cs equivalent
    public partial class App : System.Windows.Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.Run(new MainWindow());
        }
    }
}
