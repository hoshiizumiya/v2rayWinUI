using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using v2rayWinUI.ViewModels;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class GeneralSettingsPage : Page
{
    public GeneralSettingsPageViewModel ViewModel { get; } = new GeneralSettingsPageViewModel();

    public IThemeService? AppThemeService
    {
        get
        {
            try
            {
                if (Application.Current is App app)
                {
                    return app.ThemeService;
                }
            }
            catch { }

            return null;
        }
    }

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

            try
            {
                if (Application.Current is App app && app.ThemeService != null)
                {
                    IThemeService themeService = app.ThemeService;
                    themeService.SetThemeComboBoxDefaultItem(cmbTheme);
                    themeService.SetBackdropComboBoxDefaultItem(cmbBackdrop);
                }
            }
            catch { }
        };
    }

}
