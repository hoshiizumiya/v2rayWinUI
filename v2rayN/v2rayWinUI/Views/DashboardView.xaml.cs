using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace v2rayWinUI.Views;

public sealed partial class DashboardView : UserControl
{
    public event Action<string>? NavigateRequested;

    public DashboardView()
    {
        InitializeComponent();

        btnGoServers.Click += (_, _) => NavigateRequested?.Invoke("servers");
        btnGoSubs.Click += (_, _) => NavigateRequested?.Invoke("subs");
        btnGoLog.Click += (_, _) => NavigateRequested?.Invoke("log");
    }
}
