# v2rayN WinUI 3 Migration

This project is the WinUI 3 implementation of v2rayN, following the dual-UI architecture pattern used in the main v2rayN project (WPF + Avalonia).

## 项目状态

✅ **Phase 1-2 基本完成** - 主窗口和服务器列表UI已实现，可以运行

### 最近完成
- ✅ 修复 `Logging` 命名空间引用 (ServiceLib.Common)
- ✅ 实现完整的服务器列表 UI
- ✅ 添加工具栏和右键菜单
- ✅ 所有菜单命令绑定到 ViewModel
- ✅ 基础对话框和消息提示

**当前总体进度: ~30%** 📊 查看详细进度: [PROGRESS.md](PROGRESS.md)

## Project Structure

```
v2rayWinUI/
├── Views/                      # UI view components (待创建)
├── Converters/                 # Value converters
├── Styles/                     # WinUI 3 style resources
│   └── DefaultStyles.xaml      # Default styles
├── Assets/                     # Application assets
├── App.xaml                    # Application definition ✅
├── App.xaml.cs                 # Application init ✅
├── MainWindow.xaml             # Main window UI ✅
├── MainWindow.xaml.cs          # Main window logic ✅
├── README.md                   # This file
├── SETUP.md                    # Setup guide
├── PROGRESS.md                 # Detailed progress tracking
└── WinUI3-Controls-Guide.md    # Control usage guide
```

## Features Implemented

### ✅ Main Window
- Modern WinUI 3 interface with Mica backdrop
- Menu bar with organized commands
- Tab-based layout using TabView
- Status bar with connection info

### ✅ Server List View
- Toolbar with Add/Remove/Edit buttons
- Search box for filtering
- ListView with custom item template
- Column headers (Name, Address, Port, Type, etc.)
- Context menu (right-click)
- Selection handling

### ✅ Command Integration
- All menu items bound to ViewModel commands
- ReactiveUI command pattern
- Async command execution
- ContentDialog for confirmations

## Dependencies

- **Microsoft.WindowsAppSDK** (1.8.260101001): Windows App SDK for WinUI 3
- **WinUIEx** (2.9.0): Extended functionality (system tray, hotkeys)
- **ReactiveUI.WinUI** (22.3.1): MVVM framework
- **CommunityToolkit.Labs.WinUI.Controls.DataTable**: DataGrid component
- **ServiceLib**: Shared business logic (platform-independent)

## Building

### Prerequisites
- Windows 10 version 1809 (build 17763) or later
- .NET 8.0 or .NET 10
- Visual Studio 2022 with:
  - .NET Desktop Development workload
  - Windows application development workload

### Build Steps

```powershell
# 使用 Visual Studio 2022 (推荐)
1. 打开解决方案
2. 右键点击 v2rayWinUI 项目 → 设为启动项目
3. 按 F5 运行

# 或使用命令行
dotnet restore
dotnet build v2rayWinUI/v2rayWinUI.csproj
dotnet run --project v2rayWinUI
```

## Migration Status

### ✅ Phase 1: Basic Framework (COMPLETED)
- [x] Project setup with proper dependencies
- [x] ServiceLib integration
- [x] Basic MainWindow structure with TabView
- [x] UpdateViewHandler pattern implementation
- [x] Default styles resource dictionary
- [x] Mica backdrop for modern Windows 11 look

### ✅ Phase 2: Core Windows (60% COMPLETED)
- [x] MainWindow framework and layout
- [x] Server list UI with toolbar
- [x] Command bindings to ViewModel
- [x] Context menu and dialogs
- [ ] Data binding to server list
- [ ] Real-time updates via AppEvents

### 🚧 Phase 3: Feature Windows (IN PROGRESS)
- [ ] AddServerWindow (all protocols)
- [ ] OptionSettingWindow
- [ ] RoutingSettingWindow
- [ ] DNSSettingWindow
- [ ] SubSettingWindow
- [ ] GlobalHotkeySettingWindow

### ⏳ Phase 4: System Integration (PENDING)
- [ ] System tray support (using WinUIEx)
- [ ] Global hotkey support (using WinUIEx)
- [ ] Window state persistence
- [ ] Multi-monitor support
- [ ] Auto-start functionality

### ⏳ Phase 5: Styling & Polish (PENDING)
- [ ] Complete Fluent Design theme
- [ ] Dark/Light theme support
- [ ] Animations and transitions
- [ ] Accessibility improvements
- [ ] Localization

## Key WinUI 3 Differences

### 1. Controls
| WPF | WinUI 3 | Status |
|-----|---------|--------|
| `TabControl` | `TabView` | ✅ Implemented |
| `DataGrid` | `DataTable` (CommunityToolkit) | ⏳ Pending |
| `MessageBox` | `ContentDialog` | ✅ Implemented |
| `ContextMenu` | `MenuFlyout` | ✅ Implemented |

### 2. Data Binding
- Use `x:Bind` instead of `Binding` (planned)
- Compile-time type checking
- Default mode is `OneTime` (must specify `Mode=OneWay/TwoWay`)

### 3. Dialogs
- All dialogs use `ShowAsync()` instead of `ShowDialog()`
- Must set `XamlRoot` property

## Quick Start

### Run the Application
```powershell
# In Visual Studio 2022
F5 to run

# Or command line
dotnet run --project v2rayWinUI
```

### Current Functionality
- ✅ Application launches with main window
- ✅ Menu commands trigger (show placeholder dialogs)
- ✅ Toolbar buttons work
- ✅ Right-click menu on server list
- ⏳ Server data binding (not yet implemented)
- ⏳ Actual server operations (not yet implemented)

## Documentation

- 📄 `SETUP.md` - Detailed setup and configuration
- 📄 `PROGRESS.md` - Development progress tracking
- 📄 `WinUI3-Controls-Guide.md` - Control usage examples
- 📄 `README.md` - This file (project overview)

## Architecture

### Shared ServiceLib
The project references `ServiceLib.csproj` which contains:
- `MainWindowViewModel` - Main window MVVM logic
- `AppManager` - Application lifecycle management
- `Config` - Configuration models
- `ProfileItem` - Server profile models
- `Logging` - Logging functionality

This ensures code sharing across WPF, Avalonia, and WinUI 3 versions.

### Command Pattern
```csharp
// ViewModel (ServiceLib)
public ReactiveCommand<Unit, Unit> AddVmessServerCmd { get; }

// UI Binding (v2rayWinUI)
menuAddVmessServer.Click += (s, e) => 
    ViewModel?.AddVmessServerCmd.Execute().Subscribe();
```

### Update View Handler
```csharp
private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
{
    switch (action)
    {
        case EViewAction.AddServerWindow:
            // Show add server window
            return true;
    }
}
```

## Next Steps

### Priority 1 (Current Sprint)
1. ✅ Implement server list data binding
2. Create AddServerWindow for VMess protocol
3. Implement add/remove server operations

### Priority 2
1. Support all protocol types
2. Create OptionSettingWindow
3. Implement latency testing

### Priority 3
1. System tray integration
2. Global hotkey support
3. Theme and animation polish

## Contributing

When implementing new features:
1. Create XAML and code-behind in `Views/` folder
2. Follow WinUI 3 naming conventions
3. Use styles from `Styles/DefaultStyles.xaml`
4. Use async patterns for dialogs
5. Bind to ViewModels in ServiceLib
6. Test on both Windows 10 and Windows 11

## System Requirements

- Windows 10 version 1809 (build 17763) or later
- .NET 8.0 or .NET 10
- Windows App SDK 1.8+

## Notes

- ✅ Shares ServiceLib with WPF and Avalonia versions
- ✅ Modern Windows 11 features (Mica, updated controls)
- ✅ ReactiveUI for MVVM pattern
- ✅ TabView for modern tab interface
- ⏳ System tray via WinUIEx (pending)

---

**Version**: 0.3.0-alpha  
**Status**: In Development 🚧  
**Last Updated**: 2025-01-16
