using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Enums;
using ServiceLib.Handler.SysProxy;
using System.Linq;

namespace v2rayWinUI.Views;

public sealed partial class OptionSettingWindow : Window
{
    private Config? _config;

    public OptionSettingWindow()
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
        
        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 600, Height = 700 });
    }

    private void LoadSettings()
    {
        if (_config == null) return;

        // Core settings
        cmbCoreType.ItemsSource = new[] { "Xray", "v2fly", "SingBox" };
        cmbCoreType.SelectedIndex = 0;
        
        chkLogEnabled.IsChecked = _config.CoreBasicItem?.LogEnabled ?? false;
        chkMuxEnabled.IsChecked = _config.CoreBasicItem?.MuxEnabled ?? false;

        // Inbound settings
        var socksInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "socks");
        txtSocksPort.Text = socksInbound?.LocalPort.ToString() ?? "10808";
        
        var httpInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "http");
        txtHttpPort.Text = httpInbound?.LocalPort.ToString() ?? "10809";
        
        chkAllowLANConn.IsChecked = socksInbound?.AllowLANConn ?? false;

        // System proxy
        cmbSysProxyType.ItemsSource = Enum.GetValues(typeof(ESysProxyType)).Cast<ESysProxyType>().ToList();
        cmbSysProxyType.SelectedItem = _config.SystemProxyItem.SysProxyType;
        chkNotProxyLocal.IsChecked = _config.SystemProxyItem.NotProxyLocalAddress;
        txtSysProxyExceptions.Text = _config.SystemProxyItem.SystemProxyExceptions ?? string.Empty;
        txtSysProxyAdvanced.Text = _config.SystemProxyItem.SystemProxyAdvancedProtocol ?? string.Empty;
        txtCustomPacPath.Text = _config.SystemProxyItem.CustomSystemProxyPacPath ?? string.Empty;
        txtCustomScriptPath.Text = _config.SystemProxyItem.CustomSystemProxyScriptPath ?? string.Empty;
    }

    private void SetupEventHandlers()
    {
        btnSave.Click += async (s, e) => await SaveSettings();
        btnCancel.Click += (s, e) => this.Close();
    }

    private async Task SaveSettings()
    {
        if (_config == null) return;

        try
        {
            // Core settings
            if (_config.CoreBasicItem != null)
            {
                _config.CoreBasicItem.LogEnabled = chkLogEnabled.IsChecked ?? false;
                _config.CoreBasicItem.MuxEnabled = chkMuxEnabled.IsChecked ?? false;
            }

            // Inbound settings
            if (int.TryParse(txtSocksPort.Text, out int socksPort))
            {
                var socksInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "socks");
                if (socksInbound != null)
                {
                    socksInbound.LocalPort = socksPort;
                    socksInbound.AllowLANConn = chkAllowLANConn.IsChecked ?? false;
                }
            }

            if (int.TryParse(txtHttpPort.Text, out int httpPort))
            {
                var httpInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "http");
                if (httpInbound != null)
                {
                    httpInbound.LocalPort = httpPort;
                }
            }

            ApplySystemProxySettings();

            // Save to file
            await ConfigHandler.SaveConfig(_config);

            // Apply system proxy immediately (best-effort)
            _ = await SysProxyHandler.UpdateSysProxy(_config, false);
            
            await ShowMessageAsync("Success", "Settings saved successfully!");
            this.Close();
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"OptionSettingWindow SaveSettings error: {ex.Message}");
            await ShowMessageAsync("Error", $"Failed to save settings: {ex.Message}");
        }

    }

    private void ApplySystemProxySettings()
    {
        if (_config == null) return;

        if (cmbSysProxyType.SelectedItem is ESysProxyType type)
        {
            _config.SystemProxyItem.SysProxyType = type;
        }

        _config.SystemProxyItem.NotProxyLocalAddress = chkNotProxyLocal.IsChecked ?? true;
        _config.SystemProxyItem.SystemProxyExceptions = txtSysProxyExceptions.Text ?? string.Empty;
        _config.SystemProxyItem.SystemProxyAdvancedProtocol = txtSysProxyAdvanced.Text ?? string.Empty;
        _config.SystemProxyItem.CustomSystemProxyPacPath = string.IsNullOrWhiteSpace(txtCustomPacPath.Text) ? null : txtCustomPacPath.Text;
        _config.SystemProxyItem.CustomSystemProxyScriptPath = string.IsNullOrWhiteSpace(txtCustomScriptPath.Text) ? null : txtCustomScriptPath.Text;
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = (this.Content as FrameworkElement)?.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
