using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class HotkeySettingsPage : Page
{
    public HotkeySettingsPage()
    {
        InitializeComponent();

        HotkeySettingsPageOpenDialogButton.Click += (_, _) =>
        {
            // Reuse existing implementation during migration.
            var window = new v2rayWinUI.Views.GlobalHotkeySettingWindow((App.Current as v2rayWinUI.App)?.MainWindowHandler);
            window.Activate();
        };
    }
}
