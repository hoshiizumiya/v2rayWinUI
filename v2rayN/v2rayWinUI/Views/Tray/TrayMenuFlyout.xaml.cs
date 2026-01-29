using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace v2rayWinUI.Views.Tray;

public sealed partial class TrayMenuFlyout : MenuFlyout
{
    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler<int>? ProxyModeChanged;
    public event EventHandler? UpdateSubsRequested;

    public TrayMenuFlyout()
    {
        InitializeComponent();

        MenuShow.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        MenuExit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        
        MenuProxyClear.Click += (_, _) => ProxyModeChanged?.Invoke(this, 0);
        MenuProxySet.Click += (_, _) => ProxyModeChanged?.Invoke(this, 1);
        MenuProxyNothing.Click += (_, _) => ProxyModeChanged?.Invoke(this, 2);
        MenuProxyPAC.Click += (_, _) => ProxyModeChanged?.Invoke(this, 3);
        
        MenuUpdateSubs.Click += (_, _) => UpdateSubsRequested?.Invoke(this, EventArgs.Empty);
    }
}
