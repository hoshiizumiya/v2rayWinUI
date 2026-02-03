using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using v2rayWinUI.ViewModels;
using Windows.ApplicationModel.Resources;

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
            try
            {
                ResourceLoader loader = new ResourceLoader();
                ComboBoxItem[] items = new[]
                {
                    new ComboBoxItem { Content = loader.GetString("v2rayWinUI.CoreType.Xray"), Tag = "Xray" },
                    new ComboBoxItem { Content = loader.GetString("v2rayWinUI.CoreType.v2fly"), Tag = "v2fly" },
                    new ComboBoxItem { Content = loader.GetString("v2rayWinUI.CoreType.SingBox"), Tag = "SingBox" }
                };
                cmbCoreType.ItemsSource = items;
                if (cmbCoreType.SelectedIndex < 0)
                {
                    cmbCoreType.SelectedIndex = 0;
                }
            }
            catch
            {
                cmbCoreType.ItemsSource = new[] { "Xray", "v2fly", "SingBox" };
                if (cmbCoreType.SelectedIndex < 0)
                {
                    cmbCoreType.SelectedIndex = 0;
                }
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

        ActualThemeChanged += (_, _) =>
        {
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
