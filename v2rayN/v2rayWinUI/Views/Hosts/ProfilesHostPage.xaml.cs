using Microsoft.UI.Xaml.Controls;

namespace v2rayWinUI.Views.Hosts;

public sealed partial class ProfilesHostPage : Page
{
    public v2rayWinUI.Views.ProfilesView HostedView => View;

    public ProfilesHostPage()
    {
        InitializeComponent();
    }
}
