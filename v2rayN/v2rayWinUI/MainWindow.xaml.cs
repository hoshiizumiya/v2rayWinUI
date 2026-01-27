using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.ViewModels;
using ServiceLib.Common;
using ServiceLib.Events;
using v2rayWinUI.Views;
using v2rayWinUI.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Linq;
using ServiceLib.Handler.SysProxy;

namespace v2rayWinUI;

public sealed partial class MainWindow : Window
{
    private static Config? _config;
    private MainWindowViewModel? MainViewModel { get; set; }
    private ProfilesViewModel? ProfilesViewModel { get; set; }
    private ProfilesViewModel? ProfilesViewModelRef => ProfilesViewModel;
    private bool _isInitialized = false;

    private bool _isSystemProxyEnabled;

    public MainWindow()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;
        MainViewModel = new MainWindowViewModel(UpdateViewHandler);
        ProfilesViewModel = new ProfilesViewModel(UpdateViewHandler);

        // Set window title
        this.Title = $"v2rayWinUI - {Utils.GetVersion()}";

        // Initialize window
        InitializeWindow();

        try
        {
            ExtendsContentIntoTitleBar = true;
        }
        catch { }

        Activated += (_, _) =>
        {
            try
            {
                SetTitleBar(MainWindowTitleBar);
            }
            catch { }
        };

        try
        {
            MainWindowAboutButton.Click += async (_, _) => await ShowAbout();
        }
        catch { }

        // Setup UI after InitializeComponent completes
        DispatcherQueue.TryEnqueue(() =>
        {
            Initialize();
        });
    }

    private void Initialize()
    {
        if (_isInitialized)
            return;
        _isInitialized = true;

        // Setup command bindings
        SetupCommandBindings();

        // Navigate to default page
        NavigateTo("home");

        // Subscribe to events
        SubscribeToEvents();
    }

    private void InitializeWindow()
    {
        // Window initialization
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        // Custom title bar (WinUI Gallery-like).
        try
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
        }
        catch { }

        // Set default window size
        appWindow.Resize(new Windows.Graphics.SizeInt32
        {
            Width = 1200,
            Height = 800
        });
    }

    private void SetupCommandBindings()
    {
        if (navMain != null)
        {
            // default selected page
            navMain.SelectedItem = navMain.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (i.Tag as string) == "home");
        }

        if (btnQuickReload != null)
        {
            btnQuickReload.Click += (s, e) => MainViewModel?.ReloadCmd.Execute().Subscribe();
        }
        if (btnQuickProxy != null)
        {
            btnQuickProxy.Click += async (s, e) => await ToggleSystemProxyAsync();
        }

        _isSystemProxyEnabled = _config?.SystemProxyItem?.SysProxyType == ESysProxyType.ForcedChange;
        UpdateSystemProxyButtonVisual();
    }

    private void navMain_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer is NavigationViewItem nvi)
            {
                var tag = nvi.Tag as string;
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    // Keep selection in sync (also works for FooterMenuItems).
                    sender.SelectedItem = nvi;
                    NavigateTo(tag);
                }
            }
        }
        catch { }
    }

    private void NavigateTo(string tag)
    {
        try
        {
            // Ensure NavigationView selection stays in sync for programmatic navigation (e.g. Dashboard quick actions).
            try
            {
                if (navMain != null)
                {
                    NavigationViewItem? target = navMain.MenuItems
                        .OfType<NavigationViewItem>()
                        .Concat(navMain.FooterMenuItems.OfType<NavigationViewItem>())
                        .FirstOrDefault(i => (i.Tag as string) == tag);
                    if (target != null)
                    {
                        navMain.SelectedItem = target;
                    }
                }
            }
            catch { }

            var pageType = tag switch
            {
                "home" => typeof(v2rayWinUI.Views.Hosts.DashboardHostPage),
                "servers" => typeof(v2rayWinUI.Views.Hosts.ProfilesHostPage),
                "subs" => typeof(v2rayWinUI.Views.Hosts.SubSettingHostPage),
                "log" => typeof(v2rayWinUI.Views.Hosts.MsgHostPage),
                "settings" => typeof(v2rayWinUI.Views.Hosts.SettingsHostPage),
                _ => typeof(v2rayWinUI.Views.Hosts.DashboardHostPage)
            };

            if (navFrame == null)
                return;

            navFrame.Navigate(pageType);

            // Wire up page-specific behaviors after navigation.
            if (navFrame.Content is v2rayWinUI.Views.Hosts.DashboardHostPage dashHost)
            {
                dashHost.HostedView.NavigateRequested -= Dash_NavigateRequested;
                dashHost.HostedView.NavigateRequested += Dash_NavigateRequested;
            }
            else if (navFrame.Content is v2rayWinUI.Views.Hosts.ProfilesHostPage profilesHost)
            {
                profilesHost.HostedView.BindData(ProfilesViewModel);
                profilesHost.HostedView.BindMainViewModel(MainViewModel);
            }
        }
        catch { }
    }

    private void Dash_NavigateRequested(string tag)
    {
        try
        { NavigateTo(tag); }
        catch { }
    }

    private async Task ToggleSystemProxyAsync()
    {
        if (_config == null)
            return;

        try
        {
            _isSystemProxyEnabled = !_isSystemProxyEnabled;
            _config.SystemProxyItem.SysProxyType = _isSystemProxyEnabled ? ESysProxyType.ForcedChange : ESysProxyType.ForcedClear;
            await SysProxyHandler.UpdateSysProxy(_config, forceDisable: false);
        }
        finally
        {
            DispatcherQueue.TryEnqueue(UpdateSystemProxyButtonVisual);
        }
    }

    private void UpdateSystemProxyButtonVisual()
    {
        if (btnQuickProxy == null)
            return;

        btnQuickProxy.Content = new FontIcon { Glyph = "\uE8B7" };
        var tipKey = _isSystemProxyEnabled
            ? "v2rayWinUI.MainWindow.QuickProxyButton.ToolTipOn"
            : "v2rayWinUI.MainWindow.QuickProxyButton.ToolTipOff";
        if (Application.Current.Resources.TryGetValue(tipKey, out var tip) && tip is string s)
        {
            ToolTipService.SetToolTip(btnQuickProxy, s);
        }
    }

    private void SubscribeToEvents()
    {
        // Subscribe to AppEvents for UI updates
        AppEvents.DispatcherStatisticsRequested
            .AsObservable()
            .Subscribe(update =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateStatistics(update);
                });
            });

        // Best-effort: keep proxy state in sync if ServiceLib raises the request.
        AppEvents.SysProxyChangeRequested
            .AsObservable()
            .Subscribe(proxyType =>
            {
                _isSystemProxyEnabled = proxyType == ESysProxyType.ForcedChange;
                DispatcherQueue.TryEnqueue(UpdateSystemProxyButtonVisual);
            });
    }

    private void UpdateStatistics(ServerSpeedItem? speedItem)
    {
        if (speedItem == null)
            return;

        try
        {
            // Update speed display
            var upSpeed = Utils.HumanFy(speedItem.ProxyUp);
            var downSpeed = Utils.HumanFy(speedItem.ProxyDown);
            if (txtSpeed != null)
            {
                txtSpeed.Text = $"↑ {upSpeed}/s  ↓ {downSpeed}/s";
            }

            // Update connection status
            var isConnected = !string.IsNullOrEmpty(_config?.IndexId);
            if (txtRunningInfo != null)
            {
                txtRunningInfo.Text = isConnected ? "Connected" : "Not Connected";

                // Update color based on status
                if (txtRunningInfo.Parent is Border border)
                {
                    border.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        isConnected
                            ? Windows.UI.Color.FromArgb(255, 16, 124, 16)  // Green
                            : Windows.UI.Color.FromArgb(255, 196, 0, 0));  // Red
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"UpdateStatistics error: {ex.Message}");
        }
    }

    private async Task CheckUpdate()
    {
        await ShowMessageAsync("Check Update", "Update check feature coming soon!");
    }

    private async Task ShowAbout()
    {
        var version = Utils.GetVersion();
        var content = $"v2rayN WinUI3\n\nVersion: {version}\n\nA Windows client for V2Ray/Xray\n\nBased on WinUI 3";

        var dialog = new ContentDialog
        {
            Title = "About v2rayN WinUI3",
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = (this.Content as FrameworkElement)?.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = (this.Content as FrameworkElement)?.XamlRoot
        };
        await dialog.ShowAsync();
    }

    internal async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        await Task.Delay(0);

        switch (action)
        {
            case EViewAction.AddServerWindow:
            case EViewAction.AddServer2Window:
                if (obj is ProfileItem profileItem)
                {
                    var window = new AddServerWindow(profileItem);
                    window.Activate();
                    return true;
                }
                return false;

            case EViewAction.OptionSettingWindow:
                // Migrate to in-app Settings center
                NavigateTo("settings");
                return true;

            case EViewAction.RoutingSettingWindow:
                NavigateTo("settings");
                return true;

            case EViewAction.DNSSettingWindow:
                NavigateTo("settings");
                return true;

            case EViewAction.RoutingRuleSettingWindow:
            case EViewAction.RoutingRuleDetailsWindow:
                NavigateTo("settings");
                return true;

            case EViewAction.SubSettingWindow:
                var subWindow = new SubSettingWindow();
                ModalWindowHelper.ShowModal(subWindow, this, 900, 650);
                return true;

            case EViewAction.SubEditWindow:
                // reuse SubSettingWindow's add/edit logic via a lightweight dialog
                if (obj is SubItem subItem)
                {
                    var dialog = new ContentDialog
                    {
                        Title = string.IsNullOrEmpty(subItem.Id) ? "Add Subscription" : "Edit Subscription",
                        PrimaryButtonText = "Save",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.Content.XamlRoot
                    };

                    var panel = new StackPanel { Spacing = 12 };
                    var txtRemarks = new TextBox { Header = "Remarks", Text = subItem.Remarks ?? string.Empty };
                    var txtUrl = new TextBox { Header = "URL", Text = subItem.Url ?? string.Empty };
                    var chkEnabled = new CheckBox { Content = "Enabled", IsChecked = subItem.Enabled };
                    panel.Children.Add(txtRemarks);
                    panel.Children.Add(txtUrl);
                    panel.Children.Add(chkEnabled);
                    dialog.Content = panel;

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        subItem.Remarks = txtRemarks.Text;
                        subItem.Url = txtUrl.Text;
                        subItem.Enabled = chkEnabled.IsChecked ?? true;
                        if (string.IsNullOrEmpty(subItem.Id))
                        {
                            subItem.Id = Utils.GetGuid(false);
                            await ServiceLib.Helper.SQLiteHelper.Instance.InsertAsync(subItem);
                        }
                        else
                        {
                            await ServiceLib.Helper.SQLiteHelper.Instance.UpdateAsync(subItem);
                        }
                        return true;
                    }
                }
                return false;

            case EViewAction.ShowYesNo:
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Confirm",
                        Content = "Are you sure?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        XamlRoot = this.Content.XamlRoot
                    };
                    var result = await dialog.ShowAsync();
                    return result == ContentDialogResult.Primary;
                }

            case EViewAction.GlobalHotkeySettingWindow:
                NavigateTo("settings");
                return true;

            case EViewAction.CloseWindow:
                // Best-effort: close active window (used by some ServiceLib viewmodels)
                try
                {
                    this.Close();
                }
                catch
                {
                    // ignore
                }
                return true;

            case EViewAction.FullConfigTemplateWindow:
                await ShowMessageAsync("Template", "Config template window coming soon!");
                return true;

            case EViewAction.AddServerViaClipboard:
                return true;

            case EViewAction.ScanScreenTask:
                await ShowMessageAsync("Scan", "Screen scan feature coming soon!");
                return true;

            case EViewAction.DispatcherShowMsg:
                // MsgView handles its own update via its own UpdateViewHandler.
                // Keep this for compatibility.
                return true;

            case EViewAction.DispatcherRefreshServersBiz:
                // ProfilesViewModel already refreshes observable collection.
                return true;

            case EViewAction.ProfilesFocus:
                try
                {
                    navMain.SelectedItem = navMain.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (i.Tag as string) == "servers");
                }
                catch { }
                return true;

            case EViewAction.ShareSub:
                if (obj is string url && url.Length > 0)
                {
                    var dp = new DataPackage();
                    dp.SetText(url);
                    Clipboard.SetContent(dp);
                    return true;
                }
                return false;

            case EViewAction.SetClipboardData:
                if (obj is string clip && clip.Length > 0)
                {
                    var dp = new DataPackage();
                    dp.SetText(clip);
                    Clipboard.SetContent(dp);
                    return true;
                }
                return false;

            case EViewAction.ShareServer:
                if (obj is string shareUrl && shareUrl.Length > 0)
                {
                    var dp = new DataPackage();
                    dp.SetText(shareUrl);
                    Clipboard.SetContent(dp);
                    return true;
                }
                return false;

            case EViewAction.SaveFileDialog:
                // obj is ProfileItem
                if (obj is ProfileItem profile)
                {
                    var picker = new FileSavePicker();
                    picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
                    picker.SuggestedFileName = string.IsNullOrEmpty(profile.Remarks) ? "client-config" : profile.Remarks;

                    var hWnd = WindowNative.GetWindowHandle(this);
                    InitializeWithWindow.Initialize(picker, hWnd);
                    var file = await picker.PickSaveFileAsync();
                    if (file == null)
                        return false;

                    // Delegate actual file generation to ServiceLib
                    if (ProfilesViewModelRef != null)
                    {
                        await ProfilesViewModelRef.Export2ClientConfigResult(file.Path, profile);
                    }
                    return true;
                }
                return false;
        }

        return await Task.FromResult(true);
    }
}
