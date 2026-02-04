using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Common;
using ServiceLib.Handler;
using v2rayWinUI.Base;

namespace v2rayWinUI.Views;

public sealed partial class RoutingSettingWindow : ModernDialogWindow
{
    private Config? _config;

    public RoutingSettingWindow()
    {
        InitializeComponent();
        _config = AppManager.Instance.Config;

        LoadSettings();
        SetupEventHandlers();

        // Set custom title bar
        SetTitleBar(TitleBarArea);
    }

    private void LoadSettings()
    {
        if (_config?.RoutingBasicItem == null) return;

        cmbDomainStrategy.ItemsSource = new[] { "AsIs", "IPIfNonMatch", "IPOnDemand" };
        cmbDomainStrategy.SelectedItem = _config.RoutingBasicItem.DomainStrategy ?? "AsIs";

        cmbDomainMatcher.ItemsSource = new[] { "linear", "mph" };
        cmbDomainMatcher.SelectedIndex = 0;

        chkEnableRoutingAdvanced.IsChecked = false; // Default value
    }

    private void SetupEventHandlers()
    {
        btnSave.Click += async (s, e) => await SaveSettings();
        btnCancel.Click += (s, e) => CloseWithResult(false);
    }

    private async Task SaveSettings()
    {
        if (_config?.RoutingBasicItem == null) return;

        try
        {
            _config.RoutingBasicItem.DomainStrategy = cmbDomainStrategy.SelectedItem?.ToString() ?? "AsIs";

            await ConfigHandler.SaveConfig(_config);

            try
            {
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                string title = loader.GetString("v2rayWinUI.Routing.SaveSuccess.Title");
                string msg = loader.GetString("v2rayWinUI.Routing.SaveSuccess.Message");
                string ok = loader.GetString("v2rayWinUI.Common.OK");

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = msg,
                    CloseButtonText = ok,
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch
            {
                var dialog = new ContentDialog
                {
                    Title = "Success",
                    Content = "Settings saved successfully!",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            CloseWithResult(true);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"RoutingSettingWindow error: {ex.Message}");
        }
    }
}
