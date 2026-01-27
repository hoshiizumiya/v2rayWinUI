using Microsoft.UI.Xaml.Controls;

namespace v2rayWinUI.Views.Hosts;

public sealed partial class MsgHostPage : Page
{
    public v2rayWinUI.Views.MsgView HostedView => View;

    public MsgHostPage()
    {
        InitializeComponent();
    }
}
