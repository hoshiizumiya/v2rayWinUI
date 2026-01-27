using Microsoft.UI.Xaml.Controls;

namespace v2rayWinUI.Views.Hosts;

public sealed partial class SettingsHostPage : Page
{
    public v2rayWinUI.Views.Settings.SettingsView HostedView => View;

    public SettingsHostPage()
    {
        InitializeComponent();

        Loaded += (_, _) => HostedView.ForceInitialize();
    }
}
