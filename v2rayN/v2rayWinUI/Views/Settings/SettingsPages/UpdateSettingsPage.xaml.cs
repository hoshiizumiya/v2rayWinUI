using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using v2rayWinUI.ViewModels;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class UpdateSettingsPage : Page
{
    public UpdateSettingsPageViewModel ViewModel { get; } = new UpdateSettingsPageViewModel();

    public UpdateSettingsPage()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            ViewModel.RefreshGeoStatus();
        };
    }
}
