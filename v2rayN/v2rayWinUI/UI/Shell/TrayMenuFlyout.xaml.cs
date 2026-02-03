using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using v2rayWinUI.UI.Xaml;

namespace v2rayWinUI.Views.Tray;

public sealed partial class TrayMenuFlyout : Flyout
{
    private readonly v2rayWinUI.ViewModels.TrayViewModel _viewModel;

    public v2rayWinUI.ViewModels.TrayViewModel ViewModel => _viewModel;
    public int ProxyClearParameter => 0;
    public int ProxySetParameter => 1;
    public int ProxyNothingParameter => 2;
    public int ProxyPacParameter => 3;

    public TrayMenuFlyout(v2rayWinUI.ViewModels.TrayViewModel? vm = null) : this(null, vm)
    {
    }

    public TrayMenuFlyout(IServiceProvider? serviceProvider, v2rayWinUI.ViewModels.TrayViewModel? vm = null)
    {
        InitializeComponent();

        FrameworkElement? root = Content as FrameworkElement;
        if (root == null)
        {
            _viewModel = vm ?? new v2rayWinUI.ViewModels.TrayViewModel();
            return;
        }

        if (serviceProvider != null)
        {
            root.InitializeDataContext<v2rayWinUI.ViewModels.TrayViewModel>(serviceProvider);
        }

        _viewModel = vm ?? (root.DataContext as v2rayWinUI.ViewModels.TrayViewModel) ?? new v2rayWinUI.ViewModels.TrayViewModel();
        root.DataContext = _viewModel;

        if (root.FindName("MenuCloseButton") is Button closeButton)
        {
            closeButton.Click += (_, _) => { try { this.Hide(); } catch { } };
        }

        _viewModel.RequestHide += () => { try { this.Hide(); } catch { } };
    }
}
