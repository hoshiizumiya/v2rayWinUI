# v2rayWinUI Modernization / Migration Progress

> Tracking file for WinUI modernization work (Snap.Hutao-inspired structure + WinUI `x:Bind`-first), without changing `ServiceLib`.

## Principles
- MVVM: Prefer `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
- XAML: Prefer `x:Bind` over `{Binding ...}`. DataTemplates using `x:Bind` must declare `x:DataType`.
- UI: Modern look aligned with `GeneralSettingsPage` (SettingsExpander / SettingsCard, Win11 visuals).
- Windows: Modal windows should use `OverlappedPresenter.CreateForDialog()` and be centered over owner.
- Scope: Changes should remain in `v2rayWinUI` (treat `ServiceLib` as external business layer).

## Done
- Tray flyout: restructured to a Snap.Hutao-like layout and fixed event wiring using runtime `FindName`.
- Profiles page:
  - Fixed selection syncing to service VM and updated edit enable state on selection changed.
  - Migrated `ListView.ItemTemplate` and subscription repeater template from `{Binding}` to `x:Bind` with correct `x:DataType`.
- Modal helper:
  - Added centering over owner.
  - Fixed `AppWindow.Position` type usage.
- Workspace build stability:
  - Disabled CPM conflicts to allow extra projects in workspace (e.g., Snap.Hutao) without migrating their dependency management.
- Status bar x:Bind migration:
  - Migrated all footer text bindings (Inbound/Speed/RunningServer/RunningInfo) to `x:Bind StatusBar.*` with `TargetNullValue`.
  - Fixed null crashes by ensuring all StatusBarViewModel display properties initialize to non-null defaults.
  - All display update methods (RefreshServersBiz/UpdateStatistics/InboundDisplayStatus) guarded against null.
- Sentry integration (Snap.Hutao pattern):
  - Created `ExceptionHandlingService` to centralize unhandled exception capture.
  - Enhanced `Program.cs` Sentry initialization with breadcrumbs, tagging, sampling configuration.
  - Integrated lifecycle breadcrumbs (app start, exit).
- Unified modal service:
  - Created `DialServicesogService` for common dialogs.
  - Created `ModalWindowService` + `IDialogWindow` interface.
  - `DNSSettingWindow`, `RoutingSettingWindow`, `OptionSettingWindow`, `AddServerWindow` implement `IDialogWindow`.
  - `UpdateViewHandler` refactored to show settings windows as modals instead of navigation redirects.
- App exit flow:
  - Tray Exit button now routes through `AppEvents.AppExitRequested` instead of direct `App.Current.Exit()`.
  - `MainWindow` subscribes to `AppExitRequested` and handles cleanup (tray icon disposal) before exit.

## In Progress
- AddServerWindow MVVM rewrite:
  - Convert form plumbing from code-behind to ViewModel-driven state with `x:Bind`.
  - Match v2rayN behavior (validation, save, close result) while keeping `ServiceLib` untouched.

## Planned (High Priority)
1. **Speed graph + footer speed**
   - Ensure statistics events update:
     - Footer speed text (respect `DisplayRealTimeSpeed`).
     - Dashboard `SpeedGraph` (already partially hooked).

2. **Tray flyout full feature parity**
   - Add missing operations, unify duplicated UI, ensure close button hides flyout reliably.

3. **Settings page modernization**
   - Align remaining settings pages with `GeneralSettingsPage` UI patterns (SettingsExpander/SettingsCard).
   - Consolidate DNS/Routing/Option into embedded settings sections instead of separate windows.

## Notes
- Avoid importing Snap.Hutao markup extension binding patterns; keep WinUI pages `x:Bind`-first.
- When converting templates, always add `x:DataType` to avoid WMC1110/WMC1111.
- Sentry integration follows Snap.Hutao pattern: centralized ExceptionHandlingService, breadcrumbs, BeforeSend tagging.
