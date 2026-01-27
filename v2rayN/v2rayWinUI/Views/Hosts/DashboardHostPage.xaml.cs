using Microsoft.UI.Xaml.Controls;

namespace v2rayWinUI.Views.Hosts;

public sealed partial class DashboardHostPage : Page
{
    public v2rayWinUI.Views.DashboardView HostedView => View;

    public DashboardHostPage()
    {
        InitializeComponent();
    }
}
