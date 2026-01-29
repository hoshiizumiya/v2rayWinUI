using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using ReactiveUI;
using System.Reactive.Linq;
using ServiceLib.ViewModels;
using ServiceLib.Models;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Helper;
using ServiceLib.Events;
using v2rayWinUI.ViewModels;

namespace v2rayWinUI.Views;

public sealed partial class ProfilesView : Page
{
    public ProfilesViewModel? ViewModel { get; set; }
    public MainWindowViewModel? MainViewModel { get; set; }
    private ProfilesPageViewModel? PageViewModel { get; set; }

    public ProfilesView()
    {
        this.InitializeComponent();
        this.Loaded += ProfilesView_Loaded;
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
                    MainViewModel?.AddServerViaClipboardCmd.Execute().Subscribe();
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
            if (repSubGroups == null) return;

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
                if (e.Element is not ToggleButton tb) return;
                if (repSubGroups.ItemsSourceView == null) return;

                var data = repSubGroups.ItemsSourceView.GetAt(e.Index);
                if (data is not SubItem sub) return;

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
            btnEditServer.Click += (s, e) => ViewModel?.EditServerCmd.Execute().Subscribe();
        
        if (btnTestSpeed != null)
            btnTestSpeed.Click += (s, e) => ViewModel?.MixedTestServerCmd.Execute().Subscribe();

        // Context menu items
        if (menuEdit != null)
            menuEdit.Click += (s, e) => ViewModel?.EditServerCmd.Execute().Subscribe();
        
        if (menuRemove != null)
            menuRemove.Click += (s, e) => ViewModel?.RemoveServerCmd.Execute().Subscribe();
        
        if (menuSetDefault != null)
            menuSetDefault.Click += (s, e) => ViewModel?.SetDefaultServerCmd.Execute().Subscribe();
        
        // Test speed menu
        if (menuMixedTest != null)
            menuMixedTest.Click += (s, e) => ViewModel?.MixedTestServerCmd.Execute().Subscribe();
        
        if (menuTcping != null)
            menuTcping.Click += (s, e) => ViewModel?.TcpingServerCmd.Execute().Subscribe();
        
        if (menuRealPing != null)
            menuRealPing.Click += (s, e) => ViewModel?.RealPingServerCmd.Execute().Subscribe();
        
        if (menuSpeedTest != null)
            menuSpeedTest.Click += (s, e) => ViewModel?.SpeedServerCmd.Execute().Subscribe();
        
        // Export menu
        if (menuExportUrl != null)
            menuExportUrl.Click += (s, e) => ViewModel?.Export2ShareUrlCmd.Execute().Subscribe();
        
        if (menuExportClipboard != null)
            menuExportClipboard.Click += (s, e) => ViewModel?.Export2ClientConfigClipboardCmd.Execute().Subscribe();
        
        // Move menu
        if (menuMoveTop != null)
            menuMoveTop.Click += (s, e) => ViewModel?.MoveTopCmd.Execute().Subscribe();
        
        if (menuMoveUp != null)
            menuMoveUp.Click += (s, e) => ViewModel?.MoveUpCmd.Execute().Subscribe();
        
        if (menuMoveDown != null)
            menuMoveDown.Click += (s, e) => ViewModel?.MoveDownCmd.Execute().Subscribe();
        
        if (menuMoveBottom != null)
            menuMoveBottom.Click += (s, e) => ViewModel?.MoveBottomCmd.Execute().Subscribe();
        
        // Filter text changed
        if (txtServerFilter != null)
        {
            txtServerFilter.TextChanged += (s, e) =>
            {
                if (PageViewModel != null) PageViewModel.ServerFilterText = txtServerFilter.Text;
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

                // selection already synced to ServiceLib viewmodel above
            };
    }

    private void ShowAddServerMenu()
    {
        var flyout = new MenuFlyout();

        flyout.Items.Add(CreateAddServerMenuItem("VMess", EConfigType.VMess));
        flyout.Items.Add(CreateAddServerMenuItem("VLESS", EConfigType.VLESS));
        flyout.Items.Add(CreateAddServerMenuItem("Shadowsocks", EConfigType.Shadowsocks));
        flyout.Items.Add(CreateAddServerMenuItem("SOCKS", EConfigType.SOCKS));
        flyout.Items.Add(CreateAddServerMenuItem("HTTP", EConfigType.HTTP));
        flyout.Items.Add(CreateAddServerMenuItem("Trojan", EConfigType.Trojan));
        flyout.Items.Add(CreateAddServerMenuItem("Hysteria2", EConfigType.Hysteria2));
        flyout.Items.Add(CreateAddServerMenuItem("TUIC", EConfigType.TUIC));
        flyout.Items.Add(CreateAddServerMenuItem("WireGuard", EConfigType.WireGuard));
        
        if (btnAddServer != null)
            flyout.ShowAt(btnAddServer);
    }

    private MenuFlyoutItem CreateAddServerMenuItem(string text, EConfigType configType)
    {
        var item = new MenuFlyoutItem
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
                    MainViewModel?.AddVmessServerCmd.Execute().Subscribe();
                    break;
                case EConfigType.VLESS:
                    MainViewModel?.AddVlessServerCmd.Execute().Subscribe();
                    break;
                case EConfigType.Shadowsocks:
                    MainViewModel?.AddShadowsocksServerCmd.Execute().Subscribe();
                    break;
                case EConfigType.SOCKS:
                    MainViewModel?.AddSocksServerCmd.Execute().Subscribe();
                    break;
                case EConfigType.HTTP:
                    MainViewModel?.AddHttpServerCmd.Execute().Subscribe();
                    break;
                case EConfigType.Trojan:
                    MainViewModel?.AddTrojanServerCmd.Execute().Subscribe();
                    break;
                case EConfigType.Hysteria2:
                    MainViewModel?.AddHysteria2ServerCmd.Execute().Subscribe();
                    break;
                default:
                    MainViewModel?.AddVmessServerCmd.Execute().Subscribe();
                    break;
            }
        };
        return item;
    }

    private void SetChipState(ToggleButton active)
    {
        var root = Content as FrameworkElement;
        var chipAll = root?.FindName("chipAll") as ToggleButton;

        if (chipAll != null && chipAll != active) chipAll.IsChecked = false;

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
        if (active.IsChecked != true) active.IsChecked = true;
    }

    private async Task RemoveServers()
    {
        if (lstServers == null || lstServers.SelectedItems.Count == 0)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete {lstServers.SelectedItems.Count} server(s)?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel?.RemoveServerCmd.Execute().Subscribe();
        }
    }

    public void BindData(ProfilesViewModel? viewModel)
    {
        if (viewModel == null) return;
        
        ViewModel = viewModel;
        PageViewModel = new ProfilesPageViewModel(viewModel);
        if (lstServers != null)
        {
            lstServers.ItemsSource = viewModel.ProfileItems;
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
                        }
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

    public void BindMainViewModel(MainWindowViewModel? mainViewModel)
    {
        MainViewModel = mainViewModel;
        if (ViewModel != null)
        {
            PageViewModel = new ProfilesPageViewModel(ViewModel);
        }
    }
}
