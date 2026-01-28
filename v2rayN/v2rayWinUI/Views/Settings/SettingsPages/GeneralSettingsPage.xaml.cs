using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using v2rayWinUI.ViewModels;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class GeneralSettingsPage : Page
{
    public GeneralSettingsPageViewModel ViewModel { get; } = new GeneralSettingsPageViewModel();

    public GeneralSettingsPage()
    {
        InitializeComponent();

        btnSave.Visibility = Visibility.Collapsed;

        Loaded += (_, _) =>
        {
            cmbCoreType.ItemsSource = new[] { "Xray", "v2fly", "SingBox" };
            if (cmbCoreType.SelectedIndex < 0)
            {
                cmbCoreType.SelectedIndex = 0;
            }
        };
    }

}
