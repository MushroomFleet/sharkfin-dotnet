# 🦈 SharkFin Companion

A playful and sophisticated desktop companion that brings a virtual shark to your screen! Watch as your new aquatic friend swims around, follows your mouse, and exhibits realistic shark behaviors with personality-driven AI.

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-6.0--windows-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)

## ✨ Features

### 🧠 Advanced AI Behaviors
- **8 Distinct Behavioral States**: Patrol, Stalking, Alert, Circling, Hunt, Seeking, Attacking, and Eating
- **4 Unique Shark Personalities**:
  - 🔥 **Aggressive Hunter**: Fast-moving, attacks frequently
  - 🔍 **Curious Explorer**: Investigates everything on screen
  - 😴 **Lazy Drifter**: Slow, prefers rest and gentle movements
  - 👥 **Social Schooler**: Follows other sharks, great for groups

### 🌊 Diverse Swimming Patterns
- **Figure-8 Swimming**: Elegant parametric swimming patterns
- **Depth Diving**: Vertical exploration with occasional deep dives
- **Zigzag Swimming**: Dynamic diagonal movement patterns
- **Edge Exploration**: Investigates screen boundaries
- **Circular Orbiting**: Orbits around points of interest
- **Random Exploration**: Curious wandering behavior
- **Rest & Drift**: Low-energy floating and minimal movement

### 🎮 Interactive Features
- **Mouse Tracking**: Responds to mouse movement and clicks
- **Dynamic Speed Adjustment**: Smooth transitions between behavioral speeds
- **Attack Sequences**: Dramatic lunges and bite animations
- **Mouse Hiding**: Temporarily hides cursor during "successful" attacks
- **Multi-Monitor Support**: Works seamlessly across multiple displays

### 🎨 Visual Polish
- **Sprite Animation System**: 8-frame swimming animation + 12-frame bite sequence
- **Direction-Aware Graphics**: Automatically flips sprites based on movement
- **Smooth Scaling**: Dynamic size changes during attack sequences
- **Transparency Effects**: Fully transparent, click-through window
- **Energy-Based Animations**: Movement speed varies with energy levels

### ⚙️ System Integration
- **System Tray Integration**: Runs quietly in background
- **Settings Persistence**: JSON-based configuration storage
- **Energy Management**: Dynamic energy system affecting behavior
- **Performance Optimized**: 60 FPS smooth animation with minimal CPU usage

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- .NET 6.0 Runtime (Windows)

### Installation
1. Download the latest release from the releases page
2. Extract the files to your desired location
3. Run `SharkFinCompanion.exe`
4. Your shark companion will appear and start swimming!

### Building from Source
```bash
# Clone the repository
git clone [repository-url]
cd sharkfin

# Build the project
dotnet build SharkFinCompanion.csproj

# Run the application
dotnet run
```

## 🎯 Usage

### Basic Interaction
- **Mouse Movement**: Shark will detect and respond to mouse activity
- **Left/Right Click**: Triggers alert behavior and potential attacks
- **System Tray**: Right-click the shark icon for options and status

### Behavioral States
1. **Patrol Mode**: Default swimming behavior when idle
2. **Stalking**: Follows mouse at a distance when detected
3. **Alert**: Responds to mouse clicks by investigating
4. **Hunt**: Active pursuit when mouse moves during interaction
5. **Attack**: Final lunge toward mouse cursor
6. **Eating**: Successful bite with visual effects

### Energy System
- Sharks have dynamic energy levels (0-100%)
- Energy affects movement speed and behavior selection
- Low energy triggers rest periods for recovery
- Deep sleep mode activates after extended idle periods

## 🏗️ Technical Details

### Architecture
- **Framework**: WPF (Windows Presentation Foundation)
- **Target**: .NET 6.0-windows
- **Graphics**: PNG sprite-based animation system
- **Input**: Low-level Windows mouse hooks
- **Storage**: JSON configuration files

### Performance
- **Animation**: 60 FPS with 16ms frame timing
- **CPU Usage**: Minimal background processing
- **Memory**: Lightweight sprite caching system
- **Compatibility**: Multi-monitor and virtual screen support

### File Structure
```
SharkFinCompanion/
├── Graphics/               # Sprite animation files
│   ├── shark_fin_swim_*.png   # Swimming animation (8 frames)
│   └── shark_bite_*.png       # Bite animation (12 frames)
├── bin/Debug/             # Compiled application
├── obj/                   # Build artifacts
├── _arch/                 # Version archives
├── MainWindow.xaml        # UI layout
├── shark_fin_app.cs       # Main application logic
└── SharkFinCompanion.csproj # Project configuration
```

## 🔧 Configuration

Settings are automatically saved to:
```
%APPDATA%/SharkFinCompanion/settings.json
```

### Available Settings
- **Speed Multiplier**: Adjust overall movement speed
- **Behavior Preferences**: Enable/disable specific swimming patterns
- **Auto-start**: Launch with Windows (future feature)
- **Multiple Sharks**: Support for shark schools (experimental)

## 🐛 Troubleshooting

### Common Issues
- **Shark not appearing**: Check if running on correct monitor in multi-display setup
- **Performance issues**: Verify .NET 6.0 runtime is installed
- **Graphics not loading**: Ensure Graphics folder is in application directory

### System Requirements
- **OS**: Windows 10 version 1809 or later
- **RAM**: 50MB minimum
- **Storage**: 20MB for application and sprites
- **Graphics**: DirectX 9.0c compatible

## 📋 Version History

- **v1.0.1**: Latest stable release
- **v1.0.0**: Initial release version
- **v0.1.x**: Development iterations (archived)

## 🤝 Contributing

This project welcomes contributions! Areas for enhancement:
- Additional shark personalities
- New swimming behaviors
- Sound effects and audio
- Customization options
- Performance optimizations

## 📄 License

This project is available under standard software licensing terms.

## 🙏 Acknowledgments

- Sprite animations and graphics assets
- Windows API integration techniques
- WPF animation framework
- Community feedback and testing

---

**Enjoy your new shark companion! 🦈**

*For support or feature requests, please use the system tray right-click menu or contact the development team.*
