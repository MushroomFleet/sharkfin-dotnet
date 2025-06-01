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

namespace SharkFinCompanion
{
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
        private double _swimAngle = 0;
        
        private enum SharkState { Swimming, Idle, Attacking, Eating }
        private SharkState _currentState = SharkState.Swimming;
        
        private bool _mouseHidden = false;
        private IntPtr _originalCursor;
        
        // Sprite animation system
        private List<BitmapImage> _swimSprites;
        private List<BitmapImage> _biteSprites;
        private int _currentSwimFrame = 0;
        private int _currentBiteFrame = 0;
        private int _frameCounter = 0;
        private bool _biteAnimationPlaying = false;

        public MainWindow()
        {
            _instance = this;
            SetupWindow();
            SetupSharkGraphics();
            SetupTimers();
            SetupMouseHook();
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
            
            // If mouse was hidden, restore it
            if (_mouseHidden)
            {
                RestoreMouse();
            }
            
            // Reset to swimming state if attacking
            if (_currentState == SharkState.Attacking || _currentState == SharkState.Eating)
            {
                _currentState = SharkState.Swimming;
                _biteImage.Visibility = Visibility.Hidden;
                _biteAnimationPlaying = false;
            }
        }

        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            var timeSinceActivity = DateTime.Now - _lastMouseActivity;
            
            switch (_currentState)
            {
                case SharkState.Swimming:
                    if (timeSinceActivity.TotalSeconds >= 8)
                    {
                        _currentState = SharkState.Idle;
                    }
                    break;
                    
                case SharkState.Idle:
                    if (timeSinceActivity.TotalSeconds >= 10)
                    {
                        StartAttack();
                    }
                    break;
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            UpdateSharkBehavior();
            UpdateSharkPosition();
            UpdateSpriteAnimations();
        }

        private void UpdateSharkBehavior()
        {
            switch (_currentState)
            {
                case SharkState.Swimming:
                    // Follow mouse with swimming motion
                    var targetX = _currentMousePos.X - 15; // Center the fin
                    var targetY = _currentMousePos.Y + 30; // Appear below cursor
                    
                    // Smooth movement with swimming lag
                    _sharkPos.X += (targetX - _sharkPos.X) * 0.1;
                    _sharkPos.Y += (targetY - _sharkPos.Y) * 0.1;
                    
                    // Add swimming oscillation
                    _swimAngle += 0.2;
                    _sharkPos.Y += Math.Sin(_swimAngle) * 2;
                    break;
                    
                case SharkState.Idle:
                    // Slow circling motion around mouse
                    _swimAngle += 0.05;
                    var radius = 50;
                    _sharkPos.X = _currentMousePos.X + Math.Cos(_swimAngle) * radius - 15;
                    _sharkPos.Y = _currentMousePos.Y + Math.Sin(_swimAngle) * radius + 30;
                    break;
                    
                case SharkState.Attacking:
                    // Quick lunge toward mouse
                    var attackTargetX = _currentMousePos.X - 15;
                    var attackTargetY = _currentMousePos.Y - 10;
                    
                    _sharkPos.X += (attackTargetX - _sharkPos.X) * 0.3;
                    _sharkPos.Y += (attackTargetY - _sharkPos.Y) * 0.3;
                    
                    // Check if close enough to "bite"
                    var distance = Math.Sqrt(Math.Pow(_sharkPos.X - attackTargetX, 2) + 
                                           Math.Pow(_sharkPos.Y - attackTargetY, 2));
                    if (distance < 20)
                    {
                        ExecuteBite();
                    }
                    break;
            }
        }

        private void StartAttack()
        {
            _currentState = SharkState.Attacking;
            
            // Animate fin growing larger for attack
            var scaleTransform = new ScaleTransform(1, 1);
            _sharkFinImage.RenderTransform = scaleTransform;
            
            var scaleAnimation = new DoubleAnimation(1, 1.5, TimeSpan.FromMilliseconds(300));
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }

        private void ExecuteBite()
        {
            _currentState = SharkState.Eating;
            _mouseHidden = true;
            _biteAnimationPlaying = true;
            _currentBiteFrame = 0;
            
            // Hide mouse cursor
            SetCursor(IntPtr.Zero);
            
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
            
            // Reset shark size
            var scaleTransform = new ScaleTransform(1.5, 1.5);
            _sharkFinImage.RenderTransform = scaleTransform;
            var resetScale = new DoubleAnimation(1.5, 1, TimeSpan.FromMilliseconds(500));
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, resetScale);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, resetScale);
        }

        private void RestoreMouse()
        {
            _mouseHidden = false;
            
            // Restore cursor (Windows will handle this automatically)
            // The cursor becomes visible again on next movement
        }

        private void UpdateSharkPosition()
        {
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

        // Add system tray support (optional)
        private void SetupSystemTray()
        {
            // Implementation for system tray icon and controls
            // This would allow users to enable/disable the shark
        }
    }

    // App.xaml.cs equivalent
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.Run(new MainWindow());
        }
    }
}
