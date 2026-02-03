using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using ServiceLib.ViewModels;
using ServiceLib.Models;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Helper;
using ServiceLib.Events;
using v2rayWinUI.ViewModels;
using v2rayWinUI.Services;

namespace v2rayWinUI.Views;

public sealed partial class ProfilesView : Page
{
    public ProfilesViewModel? ViewModel { get; set; }
    public MainWindowViewModel? MainViewModel { get; set; }
    private ProfilesPageViewModel? PageViewModel { get; set; }
    private readonly IDialogService _dialogService;
    private readonly IExceptionReporter _exceptionReporter;

    public ProfilesView()
    {
        this.InitializeComponent();
        this.Loaded += ProfilesView_Loaded;
        _dialogService = new DialogService(() => this.XamlRoot);
        _exceptionReporter = App.Services.GetRequiredService<IExceptionReporter>();
    }

    private void ProfilesView_Loaded(object sender, RoutedEventArgs e)
    {
        EnsurePasteAccelerator();
        SetupEventHandlers();
        _ = LoadSubscriptionGroupsAsync();

        try
        {
            AppEvents.ProfilesRefreshRequested
                .AsObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    try
                    {
                        await LoadSubscriptionGroupsAsync();
                    }
                    catch { }
                });
        }
        catch { }
    }

    private void EnsurePasteAccelerator()
    {
        try
        {
            KeyboardAccelerator accelerator = new KeyboardAccelerator();
            accelerator.Key = Windows.System.VirtualKey.V;
            accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
            accelerator.Invoked += (_, args) =>
            {
                try
                {
                    MainViewModel?.AddServerViaClipboardCmd.Execute().Subscribe(
                        _ => { },
                        ex => { try { ShowError(ex); } catch { } }
                    );
                    args.Handled = true;
                }
                catch
                {
                    args.Handled = false;
                }
            };
            KeyboardAccelerators.Add(accelerator);
        }
        catch
        {
            // ignore
        }
    }

    private async Task LoadSubscriptionGroupsAsync()
    {
        try
        {
            FrameworkElement? root = Content as FrameworkElement;
            ItemsRepeater? repSubGroups = root?.FindName("repSubGroups") as ItemsRepeater;
            if (repSubGroups == null)
                return;

            if (ViewModel != null)
            {
                repSubGroups.ItemsSource = ViewModel.SubItems;
            }
            else
            {
                repSubGroups.ItemsSource = await AppManager.Instance.SubItems() ?? new List<SubItem>();
            }

            repSubGroups.ElementPrepared += (s, e) =>
            {
                if (e.Element is not ToggleButton tb)
                    return;
                if (repSubGroups.ItemsSourceView == null)
                    return;

                var data = repSubGroups.ItemsSourceView.GetAt(e.Index);
                if (data is not SubItem sub)
                    return;

                tb.Checked += (s2, e2) =>
                {
                    SetChipState(tb);
                    if (PageViewModel != null)
                    {
                        PageViewModel.SelectedSubId = sub.Id;
                    }
                };
            };
        }
        catch
        {
            // ignore
        }
    }

    private void SetupEventHandlers()
    {
        // Subscription chips (use FindName to avoid source-gen timing issues)
        var root = Content as FrameworkElement;
        var chipAll = root?.FindName("chipAll") as ToggleButton;

        if (chipAll != null)
        {
            chipAll.Checked += (s, e) =>
            {
                SetChipState(chipAll);
                PageViewModel?.SelectAllGroupsCommand.Execute(null);
            };
        }

        // Toolbar buttons
        if (btnAddServer != null)
            btnAddServer.Click += (s, e) => ShowAddServerMenu();

        if (btnRemoveServer != null)
            btnRemoveServer.Click += async (s, e) => await RemoveServers();

        if (btnEditServer != null)
            btnEditServer.Click += (s, e) => ViewModel?.EditServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (btnTestSpeed != null)
            btnTestSpeed.Click += (s, e) => ViewModel?.MixedTestServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        // Context menu items
        if (menuEdit != null)
            menuEdit.Click += (s, e) => ViewModel?.EditServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuRemove != null)
            menuRemove.Click += (s, e) => ViewModel?.RemoveServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuSetDefault != null)
            menuSetDefault.Click += (s, e) => ViewModel?.SetDefaultServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        // Test speed menu
        if (menuMixedTest != null)
            menuMixedTest.Click += (s, e) => ViewModel?.MixedTestServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuTcping != null)
            menuTcping.Click += (s, e) => ViewModel?.TcpingServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuRealPing != null)
            menuRealPing.Click += (s, e) => ViewModel?.RealPingServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuSpeedTest != null)
            menuSpeedTest.Click += (s, e) => ViewModel?.SpeedServerCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        // Export menu
        if (menuExportUrl != null)
            menuExportUrl.Click += (s, e) => ViewModel?.Export2ShareUrlCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuExportClipboard != null)
            menuExportClipboard.Click += (s, e) => ViewModel?.Export2ClientConfigClipboardCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        // Move menu
        if (menuMoveTop != null)
            menuMoveTop.Click += (s, e) => ViewModel?.MoveTopCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuMoveUp != null)
            menuMoveUp.Click += (s, e) => ViewModel?.MoveUpCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuMoveDown != null)
            menuMoveDown.Click += (s, e) => ViewModel?.MoveDownCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        if (menuMoveBottom != null)
            menuMoveBottom.Click += (s, e) => ViewModel?.MoveBottomCmd.Execute().Subscribe(_ => { }, ex => ShowError(ex));

        // Filter text changed
        if (txtServerFilter != null)
        {
            txtServerFilter.TextChanged += (s, e) =>
            {
                if (PageViewModel != null)
                    PageViewModel.ServerFilterText = txtServerFilter.Text;
            };

            txtServerFilter.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    PageViewModel?.ApplyFilterCommand.Execute(null);
                    e.Handled = true;
                }
            };
        }

        // Selection changed
        if (lstServers != null)
            lstServers.SelectionChanged += (s, e) =>
            {
                if (ViewModel != null && lstServers.SelectedItem is ProfileItemModel selected)
                {
                    ViewModel.SelectedProfile = selected;
                }

                if (ViewModel != null)
                {
                    ViewModel.SelectedProfiles = lstServers.SelectedItems.Cast<ProfileItemModel>().ToList();
                }

                SyncEditEnabledState();

                // selection already synced to ServiceLib viewmodel above
            };
    }

    private void ShowAddServerMenu()
    {
        MenuFlyout flyout = new MenuFlyout();

        // Import
        flyout.Items.Add(CreateCommandMenuItem("\u4ECE\u526A\u8D34\u677F\u5BFC\u5165\u5206\u4EAB\u94FE\u63A5", () => MainViewModel!.AddServerViaClipboardCmd));
        flyout.Items.Add(CreateCommandMenuItem("\u626B\u63CF\u5C4F\u5E55\u4E0A\u7684\u4E8C\u7EF4\u7801", () => MainViewModel!.AddServerViaScanCmd));
        flyout.Items.Add(CreateCommandMenuItem("\u626B\u63CF\u56FE\u7247\u4E2D\u7684\u4E8C\u7EF4\u7801", () => MainViewModel!.AddServerViaImageCmd));
        flyout.Items.Add(new MenuFlyoutSeparator());

        // Advanced server types
        flyout.Items.Add(CreateCommandMenuItem("\u6DFB\u52A0\u81EA\u5B9A\u4E49\u914D\u7F6E", () => MainViewModel!.AddCustomServerCmd));
        flyout.Items.Add(CreateCommandMenuItem("\u6DFB\u52A0\u7B56\u7565\u7EC4", () => MainViewModel!.AddPolicyGroupServerCmd));
        flyout.Items.Add(CreateCommandMenuItem("\u6DFB\u52A0\u94FE\u5F0F\u4EE3\u7406", () => MainViewModel!.AddProxyChainServerCmd));
        flyout.Items.Add(new MenuFlyoutSeparator());

        // Common protocols
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [VMess]", EConfigType.VMess));
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [VLESS]", EConfigType.VLESS));
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [Shadowsocks]", EConfigType.Shadowsocks));
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [Trojan]", EConfigType.Trojan));
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [WireGuard]", EConfigType.WireGuard));
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [SOCKS]", EConfigType.SOCKS));
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [HTTP]", EConfigType.HTTP));
        flyout.Items.Add(new MenuFlyoutSeparator());

        // Others
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [Hysteria2]", EConfigType.Hysteria2));
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [TUIC]", EConfigType.TUIC));
        flyout.Items.Add(CreateAddServerMenuItem("\u6DFB\u52A0 [Anytls]", EConfigType.Anytls));

        if (btnAddServer != null)
        {
            flyout.ShowAt(btnAddServer);
        }
    }

    private MenuFlyoutItem CreateAddServerMenuItem(string text, EConfigType configType)
    {
        MenuFlyoutItem item = new MenuFlyoutItem
        {
            Text = text,
            Icon = new FontIcon { Glyph = "\uE710" }
        };
        item.Click += (s, e) =>
        {
            // Use MainWindowViewModel commands that are already wired to UpdateViewHandler
            switch (configType)
            {
                case EConfigType.VMess:
                    ExecuteSafely(MainViewModel?.AddVmessServerCmd);
                    break;
                case EConfigType.VLESS:
                    ExecuteSafely(MainViewModel?.AddVlessServerCmd);
                    break;
                case EConfigType.Shadowsocks:
                    ExecuteSafely(MainViewModel?.AddShadowsocksServerCmd);
                    break;
                case EConfigType.SOCKS:
                    ExecuteSafely(MainViewModel?.AddSocksServerCmd);
                    break;
                case EConfigType.HTTP:
                    ExecuteSafely(MainViewModel?.AddHttpServerCmd);
                    break;
                case EConfigType.Trojan:
                    ExecuteSafely(MainViewModel?.AddTrojanServerCmd);
                    break;
                case EConfigType.Hysteria2:
                    ExecuteSafely(MainViewModel?.AddHysteria2ServerCmd);
                    break;
                case EConfigType.TUIC:
                    ExecuteSafely(MainViewModel?.AddTuicServerCmd);
                    break;
                case EConfigType.WireGuard:
                    ExecuteSafely(MainViewModel?.AddWireguardServerCmd);
                    break;
                case EConfigType.Anytls:
                    ExecuteSafely(MainViewModel?.AddAnytlsServerCmd);
                    break;
                default:
                    ExecuteSafely(MainViewModel?.AddVmessServerCmd);
                    break;
            }
        };
        return item;
    }

    private MenuFlyoutItem CreateCommandMenuItem(string text, Func<ReactiveCommand<Unit, Unit>> commandProvider)
    {
        MenuFlyoutItem item = new MenuFlyoutItem
        {
            Text = text,
            Icon = new FontIcon { Glyph = "\uE71D" }
        };

        item.Click += (s, e) =>
        {
            ReactiveCommand<Unit, Unit> cmd = commandProvider();
            ExecuteSafely(cmd);
        };

        return item;
    }

    private void ExecuteSafely(ReactiveCommand<Unit, Unit>? command)
    {
        try
        {
            if (command == null)
            {
                return;
            }

            command.Execute().Subscribe(
                _ => { },
                ex => { try { ShowError(ex); } catch { } });
        }
        catch (Exception ex)
        {
            try { ShowError(ex); } catch { }
        }
    }

    private void SetChipState(ToggleButton active)
    {
        var root = Content as FrameworkElement;
        var chipAll = root?.FindName("chipAll") as ToggleButton;

        if (chipAll != null && chipAll != active)
            chipAll.IsChecked = false;

        // uncheck all repeater toggles except active
        var repSubGroups = root?.FindName("repSubGroups") as ItemsRepeater;
        if (repSubGroups != null)
        {
            for (var i = 0; i < repSubGroups.ItemsSourceView.Count; i++)
            {
                if (repSubGroups.TryGetElement(i) is ToggleButton tb && tb != active)
                {
                    tb.IsChecked = false;
                }
            }
        }
        if (active.IsChecked != true)
            active.IsChecked = true;
    }

    private async Task RemoveServers()
    {
        if (lstServers == null || lstServers.SelectedItems.Count == 0)
        {
            return;
        }

        bool result = await _dialogService.ShowConfirmAsync(
            "Confirm Delete",
            $"Are you sure you want to delete {lstServers.SelectedItems.Count} server(s)?");

        if (result)
        {
            ViewModel?.RemoveServerCmd.Execute().Subscribe();
        }
    }

    public void BindData(ProfilesViewModel? viewModel)
    {
        if (viewModel == null)
            return;

        ViewModel = viewModel;
        PageViewModel = new ProfilesPageViewModel(viewModel);
        SyncEditEnabledState();
        if (lstServers != null)
        {
            lstServers.ItemsSource = viewModel.ProfileItems;
            if (viewModel.SelectedProfile != null)
            {
                lstServers.SelectedItem = viewModel.SelectedProfile;
            }
            else if (viewModel.ProfileItems.Count > 0)
            {
                ProfileItemModel firstItem = viewModel.ProfileItems.First();
                lstServers.SelectedItem = firstItem;
                viewModel.SelectedProfile = firstItem;
            }
        }

        // If items are empty, trigger a refresh
        if (viewModel.ProfileItems.Count == 0)
        {
            _ = viewModel.RefreshServers();
        }

        try
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(vm => vm != null)
                .SelectMany(vm => vm!.WhenAnyValue(x => x.ProfileItems))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(items =>
                {
                    try
                    {
                        if (lstServers != null)
                        {
                            lstServers.ItemsSource = items;
                            if (ViewModel?.SelectedProfile != null)
                            {
                                lstServers.SelectedItem = ViewModel.SelectedProfile;
                            }
                            else if (items.Count > 0)
                            {
                                lstServers.SelectedItem = items.First();
                            }
                        }

                        SyncEditEnabledState();
                    }
                    catch { }
                });
        }
        catch { }

        try
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(vm => vm != null)
                .SelectMany(vm => vm!.WhenAnyValue(x => x.SubItems))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(items =>
                {
                    try
                    {
                        _ = LoadSubscriptionGroupsAsync();
                    }
                    catch { }
                });
        }
        catch { }

        // Initial load
        _ = LoadSubscriptionGroupsAsync();
    }

    private void SyncEditEnabledState()
    {
        try
        {
            if (ViewModel == null)
            {
                return;
            }

            bool canEdit = ViewModel.SelectedProfile != null && !string.IsNullOrEmpty(ViewModel.SelectedProfile.IndexId);
            if (btnEditServer != null)
            {
                btnEditServer.IsEnabled = canEdit;
            }
            if (menuEdit != null)
            {
                menuEdit.IsEnabled = canEdit;
            }
        }
        catch { }
    }

    public void BindMainViewModel(MainWindowViewModel? mainViewModel)
    {
        MainViewModel = mainViewModel;
        if (ViewModel != null)
        {
            PageViewModel = new ProfilesPageViewModel(ViewModel);
        }
    }

    private async void ShowError(Exception? ex)
    {
        try
        {
            if (ex != null)
            {
                _exceptionReporter.Report(ex, "ProfilesView.ShowError");
            }
            string message = ex?.Message ?? "Unknown error";
            await _dialogService.ShowMessageAsync("Error", message);
        }
        catch { }
    }
}
