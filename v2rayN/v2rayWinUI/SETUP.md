# v2rayN WinUI 3 Migration - Setup Instructions

## Current Status

The v2rayWinUI project has been initialized with all necessary dependencies and basic structure. However, WinUI 3 projects require Visual Studio 2022 with specific workloads to build properly, as they rely on XAML source generation.

## What Has Been Completed

### 1. Project Configuration ✅
- Created `v2rayWinUI.csproj` with proper WinUI 3 SDK configuration
- Configured for .NET 8 targeting Windows 10.0.19041.0
- Added support for x86, x64, and ARM64 platforms

### 2. NuGet Packages ✅
Added to `Directory.Packages.props`:
- `Microsoft.WindowsAppSDK` (v1.6.241114003)
- `Microsoft.Windows.SDK.BuildTools` (v10.0.26100.1742)
- `ReactiveUI.WinUI` (v20.1.1)
- `WinUIEx` (v2.4.2)

### 3. Project Structure ✅
Created folder structure:
```
v2rayWinUI/
├── Views/              # For future UI components
├── Converters/         # For value converters
├── Styles/             # Style resources
│   └── DefaultStyles.xaml
├── Assets/             # Application assets
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
└── README.md
```

### 4. Core Files ✅

#### App.xaml
- Configured with WinUI 3 application template
- Added reference to DefaultStyles.xaml
- Includes XamlControlsResources

#### App.xaml.cs
- Integrated ServiceLib.Manager.AppManager initialization
- Proper error handling for initialization failure
- Window creation and activation

#### MainWindow.xaml
- Basic window structure with:
  - Mica backdrop for modern Windows 11 look
  - MenuBar with Servers, Subscription, and Settings menus
  - TabControl for content areas
  - Status bar

#### MainWindow.xaml.cs
- ViewModel integration (MainWindowViewModel)
- UpdateViewHandler pattern implementation
- Window sizing logic
- Placeholder handlers for all view actions

#### Styles/DefaultStyles.xaml
- Base styles for Button, TextBox, ComboBox, ToggleSwitch
- Standard font sizes and margins
- WinUI 3 native styling

## Next Steps to Build

### Option 1: Using Visual Studio 2022 (Recommended)

1. **Install Required Workloads**:
   - Open Visual Studio Installer
   - Ensure these workloads are installed:
     - `.NET Desktop Development`
     - `Windows application development` with Windows App SDK component

2. **Open in Visual Studio**:
   ```
   Open the solution file in Visual Studio 2022
   ```

3. **Restore NuGet Packages**:
   - Right-click solution → Restore NuGet Packages
   - This will restore all projects including ServiceLib

4. **Build the Project**:
   - Set v2rayWinUI as startup project
   - Build Solution (Ctrl+Shift+B)
   - The XAML compiler will generate necessary binding code

### Option 2: Command Line (With Visual Studio installed)

```powershell
# Restore all projects
dotnet restore

# Build ServiceLib first
dotnet build ServiceLib/ServiceLib.csproj

# Build v2rayWinUI
msbuild v2rayWinUI/v2rayWinUI.csproj /t:Restore,Build /p:Platform=x64
```

## Known Issues

### XAML Source Generation
WinUI 3 uses source generators for XAML. The following are normal until first successful build:
- `InitializeComponent()` not found
- XAML types (Window, Application, etc.) not recognized  
- These resolve after the first successful build in Visual Studio

### Framework Targeting
The project targets `net8.0-windows10.0.19041.0`. Ensure:
- .NET 8 SDK is installed
- Windows SDK 10.0.19041.0 or later is available

## Implementation Roadmap

### Phase 1: Basic Framework ✅ (CURRENT)
- [x] Project setup
- [x] ServiceLib integration
- [x] Basic window structure
- [x] Style system foundation

### Phase 2: Server List View
- [ ] Implement DataGrid for server list
- [ ] Server item template
- [ ] Context menu for server operations
- [ ] Drag-and-drop support
- [ ] ReactiveUI command bindings

### Phase 3: Settings Windows
- [ ] OptionSettingWindow with tabs
- [ ] RoutingSettingWindow
- [ ] DNSSettingWindow
- [ ] SubSettingWindow
- [ ] GlobalHotkeySettingWindow

### Phase 4: Server Management
- [ ] AddServerWindow (all server types)
- [ ] Server editing functionality
- [ ] Batch operations
- [ ] QR code scanning

### Phase 5: System Integration
- [ ] System tray icon (using WinUIEx)
- [ ] Global hotkeys (using WinUIEx)
- [ ] Auto-start functionality
- [ ] Minimize to tray behavior

### Phase 6: Polish
- [ ] Complete theme support (Light/Dark)
- [ ] Animations and transitions
- [ ] Localization
- [ ] Accessibility improvements
- [ ] Performance optimization

## Architecture Notes

### ServiceLib Integration
The project references `ServiceLib.csproj` which contains all business logic:
- `AppManager`: Application lifecycle management
- `MainWindowViewModel`: Main window MVVM logic
- `Config`: Configuration management
- `ProfileItem`: Server profile models

This ensures code sharing across WPF, Avalonia, and WinUI 3 versions.

### Update View Handler Pattern
The `UpdateViewHandler` method in MainWindow handles all window/dialog interactions:
```csharp
private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
{
    switch (action)
    {
        case EViewAction.AddServerWindow:
            // Show add server window
            return true;
        // ... other actions
    }
}
```

This pattern is consistent with the WPF and Avalonia versions.

### ReactiveUI Integration
Uses ReactiveUI.WinUI for MVVM:
- Commands are defined in MainWindowViewModel
- Will be bound to UI elements using `WhenActivated`
- Reactive properties trigger UI updates automatically

## Troubleshooting

### Build Errors About Missing Types
**Solution**: Build the project in Visual Studio first. XAML source generation happens during MSBuild with proper WinUI 3 targets.

### Missing ServiceLib References
**Solution**: 
```powershell
dotnet restore
dotnet build ServiceLib/ServiceLib.csproj
```

### WindowsAppSDK Not Found
**Solution**: Install Windows App SDK from Visual Studio Installer or via:
```powershell
dotnet workload install windows
```

## Resources

- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Windows App SDK](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [ReactiveUI.WinUI](https://www.reactiveui.net/docs/handbook/winui/)
- [WinUIEx on GitHub](https://github.com/dotMorten/WinUIEx)

## Contributing to Migration

When implementing new windows:
1. Create XAML and code-behind in `Views/` folder
2. Follow WinUI 3 naming conventions
3. Use styles from `Styles/DefaultStyles.xaml`
4. Implement async patterns for dialogs (`ShowAsync()`)
5. Bind to existing ViewModels in ServiceLib
6. Test on both Windows 10 and Windows 11

## Testing Checklist

Before committing new features:
- [ ] Builds without errors
- [ ] Window displays correctly
- [ ] All commands work
- [ ] Data binding functions
- [ ] Works in Light and Dark themes
- [ ] Mica backdrop displays (Windows 11)
- [ ] Window state persists
- [ ] No memory leaks in long-running scenarios
