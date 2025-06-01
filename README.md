# 🦈 SharkFinCompanion

A playful desktop companion that adds a swimming shark fin to your screen, following your mouse cursor wherever you go!

![Shark Fin Companion](https://img.shields.io/badge/platform-Windows-blue) ![.NET 6.0](https://img.shields.io/badge/.NET-6.0-purple) ![License](https://img.shields.io/badge/license-MIT-green)

## 🌊 Overview

SharkFinCompanion is a fun desktop overlay application that creates a transparent shark fin that swims around your screen, following your mouse cursor. Built with WPF and .NET 6, this application adds a touch of whimsy to your desktop experience without interfering with your work.

## ✨ Features

### 🎯 **Intelligent Mouse Tracking**
- **Multi-Monitor Support**: Seamlessly follows your cursor across multiple screens
- **Real-time Response**: Smooth tracking with natural swimming motion
- **Click-through Interface**: Never interferes with your applications

### 🎭 **Dynamic Behavior States**
- **🏊 Swimming Mode**: Actively follows your mouse with fluid movement
- **😴 Idle Mode**: Slow circling motion when mouse is stationary
- **⚡ Attack Mode**: Aggressive lunging behavior after extended idle time
- **🍽️ Eating Mode**: "Bites" the cursor and temporarily hides it

### 🎨 **Rich Animations**
- **8-frame Swimming Animation**: Smooth fin movement with realistic motion
- **12-frame Bite Animation**: Dramatic bite effect with visual feedback
- **Sprite-based Graphics**: High-quality PNG assets for crisp visuals
- **Smooth Transitions**: Fluid movement between animation states

### 🖥️ **Advanced Display Features**
- **Transparent Overlay**: Completely see-through background
- **Always on Top**: Stays visible above all applications
- **No Taskbar Icon**: Runs discretely without cluttering your taskbar
- **Full Desktop Coverage**: Works across your entire virtual desktop

## 📋 Requirements

- **Operating System**: Windows 10/11
- **Runtime**: .NET 6.0 or later
- **Memory**: ~20MB RAM usage
- **Display**: Supports single and multi-monitor setups

## 🚀 Installation

### Option 1: Download Release
1. Go to the [Releases](../../releases) page
2. Download the latest `SharkFinCompanion.zip`
3. Extract to your desired location
4. Run `SharkFinCompanion.exe`

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/yourusername/SharkFinCompanion.git
cd SharkFinCompanion

# Build the application
dotnet build SharkFinCompanion.csproj

# Run the application
dotnet run --project SharkFinCompanion.csproj
```

## 🎮 Usage

### Starting the Application
- Simply run `SharkFinCompanion.exe` or use `dotnet run`
- The shark fin will appear and begin following your mouse
- No configuration needed - it works out of the box!

### Behavior Guide

| State | Trigger | Description |
|-------|---------|-------------|
| **Swimming** | Active mouse movement | Fin follows cursor with smooth swimming motion |
| **Idle** | 8+ seconds of inactivity | Slow circular swimming around last cursor position |
| **Attack** | 10+ seconds of inactivity | Fin grows larger and lunges toward cursor |
| **Eating** | Attack reaches cursor | Bite animation plays and cursor temporarily disappears |

### Stopping the Application
- **Task Manager**: End the "Shark Fin Desktop Companion" process
- **Command Line**: Press `Ctrl+C` if running from terminal
- **System Restart**: Application doesn't auto-start

## 🛠️ Technical Details

### Architecture
- **Framework**: WPF (Windows Presentation Foundation)
- **Language**: C#
- **Target**: .NET 6.0 Windows
- **Mouse Tracking**: Win32 API Low-Level Mouse Hooks
- **Graphics**: Sprite-based animation system

### Performance
- **CPU Usage**: <1% during normal operation
- **Memory**: ~20MB RAM footprint
- **Frame Rate**: 60 FPS smooth animations
- **Startup Time**: <2 seconds

### File Structure
```
SharkFinCompanion/
├── shark_fin_app.cs          # Main application logic
├── MainWindow.xaml           # Window definition
├── SharkFinCompanion.csproj  # Project configuration
├── Graphics/                 # Sprite assets
│   ├── shark_fin_swim_01.png # Swimming animation frames (1-8)
│   ├── shark_fin_swim_02.png
│   ├── ...
│   ├── shark_bite_01.png     # Bite animation frames (1-12)
│   ├── shark_bite_02.png
│   └── ...
└── README.md                 # This file
```

## 🎨 Customization

### Adding New Sprites
1. Replace PNG files in the `Graphics/` folder
2. Maintain naming convention: `shark_fin_swim_XX.png` (01-08)
3. Bite animations: `shark_bite_XX.png` (01-12)
4. Recommended size: Swimming (32x24px), Bite (64x64px)

### Modifying Behavior
Edit `shark_fin_app.cs` to adjust:
- **Timing**: Change idle and attack trigger durations
- **Movement**: Modify swimming speed and following behavior  
- **Animation**: Adjust frame rates and sprite cycling

## 🤝 Contributing

We welcome contributions! Here's how you can help:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Setup
```bash
# Install .NET 6.0 SDK
# Clone your fork
git clone https://github.com/yourusername/SharkFinCompanion.git

# Restore dependencies
dotnet restore

# Build and test
dotnet build
dotnet run
```

## 🐛 Troubleshooting

### Common Issues

**Shark fin not appearing**
- Ensure .NET 6.0 runtime is installed
- Check Windows permissions for the application
- Verify Graphics folder contains all required PNG files

**Poor performance on multiple monitors**
- Update graphics drivers
- Close other overlay applications
- Check system resources in Task Manager

**Shark fin stuck on one monitor**
- Restart the application
- Verify virtual desktop settings in Windows display configuration

### Getting Help
- 📫 **Issues**: Report bugs via [GitHub Issues](../../issues)
- 💬 **Discussions**: Join conversations in [GitHub Discussions](../../discussions)
- 📧 **Contact**: Reach out for direct support

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by classic desktop pet applications
- Built with love for the developer community
- Thanks to all contributors and users!

---

**Made with 🦈 and ❤️**

*Keep swimming!* 🌊
