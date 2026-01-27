using Microsoft.UI.Xaml.Controls;

namespace v2rayWinUI.Views.Hosts;

public sealed partial class SubSettingHostPage : Page
{
    public v2rayWinUI.Views.SubSettingView HostedView => View;

    public SubSettingHostPage()
    {
        InitializeComponent();
    }
}
