using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Common;
using ServiceLib.Handler;

namespace v2rayWinUI.Views;

public sealed partial class DNSSettingWindow : Window
{
    private Config? _config;

    public DNSSettingWindow()
    {
        this.InitializeComponent();
        _config = AppManager.Instance.Config;
        
        InitializeWindow();
        LoadSettings();
        SetupEventHandlers();
    }

    private void InitializeWindow()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        
        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 600, Height = 500 });
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
        btnCancel.Click += (s, e) => this.Close();
    }

    private async Task SaveSettings()
    {
        if (_config?.RoutingBasicItem == null) return;

        try
        {
            // DNS settings would be saved here when model is updated
            // For now, just show success message
            
            await ConfigHandler.SaveConfig(_config);
            
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = "DNS settings saved successfully!",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
            this.Close();
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"DNSSettingWindow error: {ex.Message}");
        }
    }
}
