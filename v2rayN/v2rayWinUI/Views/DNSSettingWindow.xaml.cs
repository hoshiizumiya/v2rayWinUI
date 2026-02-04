using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Common;
using ServiceLib.Handler;
using v2rayWinUI.Base;

namespace v2rayWinUI.Views;

public sealed partial class DNSSettingWindow : ModernDialogWindow
{
    private Config? _config;

    public DNSSettingWindow()
    {
        this.InitializeComponent();
        _config = AppManager.Instance.Config;

        LoadSettings();
        SetupEventHandlers();

        // Set custom title bar
        SetTitleBar(TitleBarArea);
    }

    private void LoadSettings()
    {
        // DNS settings are stored in RoutingBasicItem in this model
        if (_config?.RoutingBasicItem == null) return;

        chkUseSystemHosts.IsChecked = false; // Default
        txtNormalDNS.Text = string.Empty;
        txtTunnelDNS.Text = string.Empty;
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
            await ConfigHandler.SaveConfig(_config);

            try
            {
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                string title = loader.GetString("v2rayWinUI.DNS.SaveSuccess.Title");
                string msg = loader.GetString("v2rayWinUI.DNS.SaveSuccess.Message");
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
                    Content = "DNS settings saved successfully!",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            CloseWithResult(true);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"DNSSettingWindow error: {ex.Message}");
        }
    }
}
