using Microsoft.Extensions.DependencyInjection;
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
using v2rayWinUI.Services;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace v2rayWinUI.Views;

public sealed partial class MainView : Page
{
    private readonly Config _config;
    private readonly IDialogService _dialogService;
    private readonly IModalWindowService _modalWindowService;
    private readonly IExceptionReporter _exceptionReporter;
    private readonly string[] _systemProxyOptions = new string[] { "Clear", "Set", "Nothing", "PAC" };
    internal MainWindowViewModel? _mainViewModel;
    internal ProfilesViewModel? _profilesViewModel;
    internal SubSettingViewModel? _subSettingViewModel;
    internal StatusBarViewModel? _statusBarViewModel;

    public string[] SystemProxyOptions => _systemProxyOptions;

    public StatusBarViewModel StatusBar
    {
        get
        {
            // Use the ServiceLib singleton so all updates come from same instance
            StatusBarViewModel inst = ServiceLib.ViewModels.StatusBarViewModel.Instance;
            // ensure updateView is set
            try
            { inst.InitUpdateView(UpdateViewHandler); }
            catch { }
            _statusBarViewModel = inst;
            return _statusBarViewModel;
        }
    }
    private bool _isInitialized;
    private readonly Dictionary<string, Type> _pageMap = new();

    public MainView()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;
        _dialogService = new DialogService(GetDialogXamlRoot);
        _modalWindowService = new ModalWindowService();
        _exceptionReporter = App.Services.GetRequiredService<IExceptionReporter>();
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

        try
        {
            if (Application.Current is App app)
            {
                app.SetMainWindowHandler(UpdateViewHandler);
            }
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.Initialize.AppHandler");
        }

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
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.Initialize.NavSelection");
        }

        // Defer initial navigation until navFrame is loaded.
        try
        {
            if (navFrame != null)
            {
                navFrame.Loaded -= NavFrame_Loaded;
                navFrame.Loaded += NavFrame_Loaded;
            }
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.Initialize.NavFrameLoaded");
        }

        try
        {
            _ = StatisticsManager.Instance.Init(_config, async update =>
            {
                AppEvents.DispatcherStatisticsRequested.Publish(update);
                await Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.Initialize.Statistics");
        }
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
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.NavSelectionChanged");
        }
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
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.SetupCommandBindings");
        }
    }

    private void SetupStatusBarBindings()
    {
        try
        {
            _statusBarViewModel = StatusBar;

            try
            {
                // Avoid async PropertyChanged arriving on background thread; keep x:Bind notifications on UI thread.
                StatusBar.PropertyChanged += (_, e) =>
                {
                    try
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            // noop: x:Bind will re-read properties; this ensures it happens on UI thread.
                        });
                    }
                    catch { }
                };
            }
            catch { }

            AppEvents.InboundDisplayRequested.Publish();
            try
            { AppEvents.ProfilesRefreshRequested.Publish(); }
            catch { }
            try
            { AppEvents.RoutingsMenuRefreshRequested.Publish(); }
            catch { }
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.SetupStatusBarBindings");
        }
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
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.SubscribeToEvents");
        }
    }

    private void UpdateStatistics(ServerSpeedItem? speedItem)
    {
        if (speedItem is null)
        {
            return;
        }

        try
        {
            // Keep ServiceLib singleton as the single source of truth for footer bindings.
            _ = StatusBar.UpdateStatistics(speedItem);
        }
        catch
        {
            // ignored
        }
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
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.NavigateTo");
        }
    }

    private void OnRootFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        try
        {
            if (MainWindowTitleBar != null)
            {
                MainWindowTitleBar.IsBackButtonVisible = navFrame?.CanGoBack == true;
            }

            SyncNavigationSelection(e.SourcePageType);
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainView.OnRootFrameNavigated");
        }
    }

    private void SyncNavigationSelection(Type? pageType)
    {
        if (pageType == null || navMain == null)
        {
            return;
        }

        string? tag = _pageMap.FirstOrDefault(pair => pair.Value == pageType).Key;
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        NavigationViewItem? target = navMain.MenuItems
            .OfType<NavigationViewItem>()
            .Concat(navMain.FooterMenuItems.OfType<NavigationViewItem>())
            .FirstOrDefault(item => (item.Tag as string) == tag);
        if (target != null)
        {
            navMain.SelectedItem = target;
        }
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

        XamlRoot? dialogRoot = GetDialogXamlRoot();
        if (dialogRoot == null)
        {
            return;
        }

        ContentDialog dialog = new ContentDialog
        {
            Title = "About v2rayN WinUI3",
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = dialogRoot
        };
        await dialog.ShowAsync();
    }

    private XamlRoot? GetDialogXamlRoot()
    {
        try
        {
            if (XamlRoot != null)
            {
                return XamlRoot;
            }

            if (v2rayWinUI.App.StartupWindow is MainWindow mainWindow && mainWindow.Content is FrameworkElement root)
            {
                return root.XamlRoot;
            }
        }
        catch { }

        return null;
    }

    private async Task<bool> ShowSubEditDialog(SubItem subItem)
    {
        ResourceLoader loader = new();
        string hdrRemarks = loader.GetString("v2rayWinUI.MainView.SubEdit.Remarks");
        string hdrUrl = loader.GetString("v2rayWinUI.MainView.SubEdit.URL");
        string lblEnabled = loader.GetString("v2rayWinUI.MainView.SubEdit.Enabled");

        TextBox remarksBox = new TextBox { Header = hdrRemarks, Text = subItem.Remarks ?? string.Empty, Style = (Style)Application.Current.Resources["DefTextBox"] };
        TextBox urlBox = new TextBox { Header = hdrUrl, Text = subItem.Url ?? string.Empty, Style = (Style)Application.Current.Resources["DefTextBox"], AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        CheckBox enabledCheck = new CheckBox { Content = lblEnabled, IsChecked = subItem.Enabled };

        StackPanel stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(remarksBox);
        stack.Children.Add(urlBox);
        stack.Children.Add(enabledCheck);

        string addTitle = loader.GetString("v2rayWinUI.MainView.SubEdit.AddTitle");
        string editTitle = loader.GetString("v2rayWinUI.MainView.SubEdit.EditTitle");
        string save = loader.GetString("v2rayWinUI.Common.Save");
        string cancel = loader.GetString("v2rayWinUI.Common.Cancel");

        ContentDialogResult result = await _dialogService.ShowDialogAsync(
            string.IsNullOrEmpty(subItem.Id) ? addTitle : editTitle,
            stack,
            save,
            string.Empty,
            cancel);

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
                if (obj is ProfileItem profileItem)
                {
                    if (owner != null)
                    {
                        return await _modalWindowService.ShowModalAsync<AddServerWindow>(owner, profileItem, 1000, 800);
                    }

                    AddServerWindow window = new AddServerWindow(profileItem);
                    return await window.ShowDialogAsync(null, 1000, 800);
                }
                return false;

            case EViewAction.AddServer2Window:
                if (obj is ProfileItem profileItem2)
                {
                    if (owner != null)
                    {
                        return await _modalWindowService.ShowModalAsync<AddServer2Window>(owner, profileItem2, 1000, 800);
                    }

                    AddServer2Window window2 = new AddServer2Window(profileItem2);
                    return await window2.ShowDialogAsync(null, 1000, 800);
                }
                return false;

            case EViewAction.AddGroupServerWindow:
                if (obj is ProfileItem groupProfileItem)
                {
                    if (owner != null)
                    {
                        return await _modalWindowService.ShowModalAsync<AddServerWindow>(owner, groupProfileItem, 1000, 800);
                    }

                    AddServerWindow window = new AddServerWindow(groupProfileItem);
                    return await window.ShowDialogAsync(null, 1000, 800);
                }
                return false;

            case EViewAction.DNSSettingWindow:
                if (owner != null)
                {
                    return await _modalWindowService.ShowModalAsync<DNSSettingWindow>(owner, 700, 600);
                }
                return false;

            case EViewAction.RoutingSettingWindow:
                if (owner != null)
                {
                    return await _modalWindowService.ShowModalAsync<RoutingSettingWindow>(owner, 700, 600);
                }
                return false;

            case EViewAction.OptionSettingWindow:
                if (owner != null)
                {
                    return await _modalWindowService.ShowModalAsync<OptionSettingWindow>(owner, 700, 800);
                }
                return false;

            case EViewAction.SubSettingWindow:
                NavigateTo("subs");
                return true;

            case EViewAction.SubEditWindow:
                if (obj is SubItem subItem)
                {
                    return await ShowSubEditDialog(subItem);
                }
                return false;

            case EViewAction.ShowYesNo:
                {
                    ResourceLoader loader2 = new();
                    string title2 = loader2.GetString("v2rayWinUI.Common.Confirm");
                    string content2 = obj?.ToString() ?? loader2.GetString("v2rayWinUI.Common.AreYouSure");
                    bool result = await _dialogService.ShowConfirmAsync(title2, content2);
                    return result;
                }

            case EViewAction.ShareSub:
                if (obj is string url)
                {
                    DataPackage dp = new DataPackage();
                    dp.SetText(url);
                    Clipboard.SetContent(dp);
                    try
                    {
                        ResourceLoader loader = new();
                        string msg = loader.GetString("v2rayWinUI.MainView.Share.URLCopied");
                        NoticeManager.Instance.Enqueue(msg);
                    }
                    catch
                    {
                        NoticeManager.Instance.Enqueue("URL copied to clipboard");
                    }
                    return true;
                }
                return false;

            case EViewAction.AddBatchRoutingRulesYesNo:
                {
                    ResourceLoader loader3 = new();
                    string title3 = loader3.GetString("v2rayWinUI.MainView.ImportRouting.Title");
                    string content3 = loader3.GetString("v2rayWinUI.MainView.ImportRouting.Content");
                    bool result = await _dialogService.ShowConfirmAsync(title3, content3);
                    return result;
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
