using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Events;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using v2rayWinUI.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace v2rayWinUI.Views;

public sealed partial class MainView : Page
{
    private readonly Config _config;
    internal MainWindowViewModel? _mainViewModel;
    internal ProfilesViewModel? _profilesViewModel;
    internal SubSettingViewModel? _subSettingViewModel;
    internal StatusBarViewModel? _statusBarViewModel;
    private bool _isInitialized;
    private readonly Dictionary<string, Type> _pageMap = new();

    public MainView()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;
        _mainViewModel = new MainWindowViewModel(UpdateViewHandler);
        _profilesViewModel = new ProfilesViewModel(UpdateViewHandler);
        _subSettingViewModel = new SubSettingViewModel(UpdateViewHandler);

        Loaded += (_, _) => Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        SetupCommandBindings();
        SetupStatusBarBindings();
        SubscribeToEvents();

        InitializePageMap();

        try
        {
            if (navMain != null)
            {
                NavigationViewItem? homeItem = navMain.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (i.Tag as string) == "home");
                if (homeItem != null)
                {
                    navMain.SelectedItem = homeItem;
                }
            }
        }
        catch { }

        // Defer initial navigation until navFrame is loaded.
        try
        {
            if (navFrame != null)
            {
                navFrame.Loaded -= NavFrame_Loaded;
                navFrame.Loaded += NavFrame_Loaded;
            }
        }
        catch { }

        try
        {
            if (_config.GuiItem.EnableStatistics || _config.GuiItem.DisplayRealTimeSpeed)
            {
                _ = StatisticsManager.Instance.Init(_config, async update =>
                {
                    AppEvents.DispatcherStatisticsRequested.Publish(update);
                    await Task.CompletedTask;
                });
            }
        }
        catch { }
    }

    private void InitializePageMap()
    {
        if (_pageMap.Count > 0)
        {
            return;
        }

        _pageMap["home"] = typeof(DashboardView);
        _pageMap["servers"] = typeof(ProfilesView);
        _pageMap["subs"] = typeof(SubSettingView);
        _pageMap["log"] = typeof(MsgView);
        _pageMap["settings"] = typeof(Settings.SettingsView);

        try
        {
            if (MainWindowTitleBar != null)
            {
                MainWindowTitleBar.IsBackButtonVisible = navFrame?.CanGoBack == true;
                MainWindowTitleBar.BackRequested -= TitleBar_BackRequested;
                MainWindowTitleBar.BackRequested += TitleBar_BackRequested;
            }
        }
        catch { }
    }

    private void NavFrame_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (navFrame != null)
            {
                navFrame.Loaded -= NavFrame_Loaded;
            }
        }
        catch { }

        // Prime view models so first navigation shows data without requiring manual refresh.
        try
        {
            AppEvents.SubscriptionsRefreshRequested.Publish();
            AppEvents.ProfilesRefreshRequested.Publish();
        }
        catch { }

        NavigateTo("home");
    }

    private void navMain_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        try
        {
            if (args.SelectedItemContainer is NavigationViewItem nvi)
            {
                string? tag = nvi.Tag as string;
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    NavigateTo(tag);
                }
            }
        }
        catch { }
    }

    private void SetupCommandBindings()
    {
        try
        {
            if (btnQuickReload != null)
            {
                btnQuickReload.Click += (_, _) => _mainViewModel?.ReloadCmd.Execute().Subscribe();
            }

            if (MainWindowAboutButton != null)
            {
                MainWindowAboutButton.Click += async (_, _) => await ShowAboutAsync();
            }
        }
        catch { }
    }

    private void SetupStatusBarBindings()
    {
        try
        {
            _statusBarViewModel = new StatusBarViewModel(UpdateViewHandler);
            _statusBarViewModel.PropertyChanged += (_, __) =>
            {
                try
                {
                    if (txtInbound != null)
                    {
                        txtInbound.Text = _statusBarViewModel.InboundDisplay ?? string.Empty;
                    }
                    if (txtSpeed != null)
                    {
                        txtSpeed.Text = _statusBarViewModel.SpeedProxyDisplay ?? "↑ 0 B/s  ↓ 0 B/s";
                    }
                    if (txtRunningInfo != null)
                    {
                        txtRunningInfo.Text = _statusBarViewModel.RunningInfoDisplay ?? txtRunningInfo.Text;
                    }
                    if (txtRunningServer != null)
                    {
                        txtRunningServer.Text = _statusBarViewModel.RunningServerDisplay ?? "-";
                        ToolTipService.SetToolTip(txtRunningServer, _statusBarViewModel.RunningServerToolTipText ?? "-");
                    }
                }
                catch { }
            };

            if (FooterSystemProxyCombo != null)
            {
                List<string> sysProxyModes = new List<string> { "Clear", "Set", "Nothing", "PAC" };
                FooterSystemProxyCombo.ItemsSource = sysProxyModes;
                FooterSystemProxyCombo.SelectedIndex = _statusBarViewModel.SystemProxySelected;
                FooterSystemProxyCombo.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        _statusBarViewModel.SystemProxySelected = FooterSystemProxyCombo.SelectedIndex;
                    }
                    catch { }
                };
            }

            if (FooterTunToggle != null)
            {
                FooterTunToggle.IsOn = _statusBarViewModel.EnableTun;
                FooterTunToggle.Toggled += (_, _) =>
                {
                    try
                    {
                        _statusBarViewModel.EnableTun = FooterTunToggle.IsOn;
                    }
                    catch { }
                };
            }

            if (btnCopyProxyCmd != null)
            {
                btnCopyProxyCmd.Click += (_, _) =>
                {
                    try
                    {
                        _statusBarViewModel.CopyProxyCmdToClipboardCmd.Execute().Subscribe();
                    }
                    catch { }
                };
            }

            if (btnSubUpdate != null)
            {
                btnSubUpdate.Click += (_, _) =>
                {
                    try
                    {
                        _statusBarViewModel.SubUpdateCmd.Execute().Subscribe();
                    }
                    catch { }
                };
            }

            AppEvents.InboundDisplayRequested.Publish();
        }
        catch { }
    }

    private void SubscribeToEvents()
    {
        try
        {
            AppEvents.DispatcherStatisticsRequested
                .AsObservable()
                .Subscribe(update =>
                {
                    DispatcherQueue.TryEnqueue(() => UpdateStatistics(update));
                });
        }
        catch { }
    }

    private void UpdateStatistics(ServerSpeedItem? speedItem)
    {
        if (speedItem == null)
        {
            return;
        }

        string upSpeed = Utils.HumanFy(speedItem.ProxyUp);
        string downSpeed = Utils.HumanFy(speedItem.ProxyDown);

        if (txtSpeed != null)
        {
            txtSpeed.Text = $"↑ {upSpeed}/s  ↓ {downSpeed}/s";
        }

        try
        {
            // SpeedGraph is hosted in DashboardView; footer is updated here.
        }
        catch { }
    }

    private void NavigateTo(string tag)
    {
        try
        {
            if (navFrame == null)
            {
                return;
            }

            if (!_pageMap.TryGetValue(tag, out Type? pageType))
            {
                pageType = typeof(DashboardView);
            }

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

            navFrame.Navigate(pageType);

            if (navFrame.Content is DashboardView dashView)
            {
                dashView.NavigateRequested -= Dash_NavigateRequested;
                dashView.NavigateRequested += Dash_NavigateRequested;
            }
            else if (navFrame.Content is ProfilesView profilesView)
            {
                profilesView.BindData(_profilesViewModel);
                profilesView.BindMainViewModel(_mainViewModel);
            }
            else if (navFrame.Content is SubSettingView subView)
            {
                subView.BindData(_subSettingViewModel);
            }
            else if (navFrame.Content is Settings.SettingsView settingsView)
            {
                settingsView.ForceInitialize();
            }
        }
        catch { }
    }

    private void OnRootFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        try
        {
            if (MainWindowTitleBar != null)
            {
                MainWindowTitleBar.IsBackButtonVisible = navFrame?.CanGoBack == true;
            }
        }
        catch { }
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        try
        {
            if (navFrame?.CanGoBack == true)
            {
                navFrame.GoBack();
            }
        }
        catch { }
    }

    private void Dash_NavigateRequested(string tag)
    {
        try
        {
            NavigateTo(tag);
        }
        catch { }
    }

    private void TryNavigateSettingsSection(string tag)
    {
        try
        {
            if (navFrame?.Content is Settings.SettingsView settingsView)
            {
                settingsView.NavigateToSection(tag);
                return;
            }

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
            {
                await Task.Delay(50);
                try
                {
                    if (navFrame?.Content is Settings.SettingsView settingsView2)
                    {
                        settingsView2.NavigateToSection(tag);
                    }
                }
                catch { }
            });
        }
        catch { }
    }

    private async Task ShowAboutAsync()
    {
        string version = Utils.GetVersion();
        string content = $"v2rayN WinUI3\n\nVersion: {version}\n\nA Windows client for V2Ray/Xray\n\nBased on WinUI 3";

        ContentDialog dialog = new ContentDialog
        {
            Title = "About v2rayN WinUI3",
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async Task<bool> ShowSubEditDialog(SubItem subItem)
    {
        var remarksBox = new TextBox { Header = "Remarks", Text = subItem.Remarks ?? string.Empty, Style = (Style)Application.Current.Resources["DefTextBox"] };
        var urlBox = new TextBox { Header = "URL", Text = subItem.Url ?? string.Empty, Style = (Style)Application.Current.Resources["DefTextBox"], AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        var enabledCheck = new CheckBox { Content = "Enabled", IsChecked = subItem.Enabled };

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(remarksBox);
        stack.Children.Add(urlBox);
        stack.Children.Add(enabledCheck);

        var dialog = new ContentDialog
        {
            Title = string.IsNullOrEmpty(subItem.Id) ? "Add Subscription" : "Edit Subscription",
            Content = stack,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot,
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            subItem.Remarks = remarksBox.Text;
            subItem.Url = urlBox.Text;
            subItem.Enabled = enabledCheck.IsChecked == true;

            await ConfigHandler.AddSubItem(_config, subItem);
            return true;
        }

        return false;
    }

    internal async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        await Task.Delay(0);

        MainWindow? owner = null;
        try
        {
            owner = v2rayWinUI.App.StartupWindow as MainWindow;
        }
        catch { }

        switch (action)
        {
            case EViewAction.AddServerWindow:
            case EViewAction.AddServer2Window:
                if (obj is ProfileItem profileItem)
                {
                    AddServerWindow window = new AddServerWindow(profileItem);
                    if (owner != null)
                    {
                        try
                        {
                            ModalWindowHelper.ShowModal(window, owner, 1000, 800);
                        }
                        catch
                        {
                            try { window.Activate(); } catch { }
                        }
                    }
                    else
                    {
                        try { window.Activate(); } catch { }
                    }
                    return true;
                }
                return false;

            case EViewAction.AddGroupServerWindow:
                if (obj is ProfileItem groupProfileItem)
                {
                    AddServerWindow window = new AddServerWindow(groupProfileItem);
                    if (owner != null)
                    {
                        try
                        {
                            ModalWindowHelper.ShowModal(window, owner, 1000, 800);
                        }
                        catch
                        {
                            try { window.Activate(); } catch { }
                        }
                    }
                    else
                    {
                        try { window.Activate(); } catch { }
                    }
                    return true;
                }
                return false;

            case EViewAction.OptionSettingWindow:
                NavigateTo("settings");
                TryNavigateSettingsSection("general");
                return true;

            case EViewAction.RoutingSettingWindow:
                NavigateTo("settings");
                TryNavigateSettingsSection("routing");
                return true;

            case EViewAction.DNSSettingWindow:
                NavigateTo("settings");
                TryNavigateSettingsSection("dns");
                return true;

            case EViewAction.SubSettingWindow:
                NavigateTo("subs");
                return true;

            case EViewAction.SubEditWindow:
                if (obj is SubItem subItem)
                {
                    SubSettingWindow window = new SubSettingWindow();
                    // Since SubSettingWindow just contains SubSettingView, 
                    // and SubSettingView doesn't have an "Edit" mode yet in WinUI3,
                    // we might need a separate SubEditWindow or modify SubSettingView.
                    // For now, let's at least show the window or a message.
                    // Actually, let's create a ContentDialog for simple editing if possible.
                    return await ShowSubEditDialog(subItem);
                }
                return false;

            case EViewAction.ShowYesNo:
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Confirm",
                        Content = obj?.ToString() ?? "Are you sure?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        XamlRoot = XamlRoot
                    };
                    ContentDialogResult result = await dialog.ShowAsync();
                    return result == ContentDialogResult.Primary;
                }

            case EViewAction.ShareSub:
                if (obj is string url)
                {
                    DataPackage dp = new DataPackage();
                    dp.SetText(url);
                    Clipboard.SetContent(dp);
                    NoticeManager.Instance.Enqueue("URL copied to clipboard");
                    return true;
                }
                return false;

            case EViewAction.AddBatchRoutingRulesYesNo:
                {
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "Import routing rules",
                        Content = "Add routing rules from clipboard?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        XamlRoot = XamlRoot
                    };
                    ContentDialogResult result = await dialog.ShowAsync();
                    return result == ContentDialogResult.Primary;
                }

            case EViewAction.AddServerViaClipboard:
                {
                    DataPackageView dp = Clipboard.GetContent();
                    if (dp != null && dp.Contains(StandardDataFormats.Text))
                    {
                        string text = await dp.GetTextAsync();
                        _mainViewModel?.AddServerViaClipboardCmd.Execute().Subscribe();
                        return !string.IsNullOrWhiteSpace(text);
                    }
                    return false;
                }

            case EViewAction.SaveFileDialog:
                if (obj is ProfileItem profile)
                {
                    FileSavePicker picker = new FileSavePicker();
                    picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
                    picker.SuggestedFileName = string.IsNullOrEmpty(profile.Remarks) ? "client-config" : profile.Remarks;

                    if (owner != null)
                    {
                        IntPtr hWnd = WindowNative.GetWindowHandle(owner);
                        InitializeWithWindow.Initialize(picker, hWnd);
                    }

                    Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
                    if (file == null)
                    {
                        return false;
                    }

                    if (_profilesViewModel != null)
                    {
                        await _profilesViewModel.Export2ClientConfigResult(file.Path, profile);
                    }
                    return true;
                }
                return false;
        }

        return true;
    }
}
