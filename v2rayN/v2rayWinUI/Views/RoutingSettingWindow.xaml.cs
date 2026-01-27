using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Common;
using ServiceLib.Handler;

namespace v2rayWinUI.Views;

public sealed partial class RoutingSettingWindow : Window
{
    private Config? _config;

    public RoutingSettingWindow()
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
        btnCancel.Click += (s, e) => this.Close();
    }

    private async Task SaveSettings()
    {
        if (_config?.RoutingBasicItem == null) return;

        try
        {
            _config.RoutingBasicItem.DomainStrategy = cmbDomainStrategy.SelectedItem?.ToString() ?? "AsIs";

            await ConfigHandler.SaveConfig(_config);
            
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = "Settings saved successfully!",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
            this.Close();
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"RoutingSettingWindow error: {ex.Message}");
        }
    }
}
