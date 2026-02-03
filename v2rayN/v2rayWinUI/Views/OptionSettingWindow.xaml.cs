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

public sealed partial class OptionSettingWindow : Window, Services.IDialogWindow
{
    private Config? _config;
    private TaskCompletionSource<bool>? _closeCompletionSource;
    private bool _dialogResult;

    public OptionSettingWindow()
    {
        this.InitializeComponent();
        _config = AppManager.Instance.Config;

        LoadSettings();
        SetupEventHandlers();

        Closed += (_, _) => CompleteDialogResult();
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
        try
        {
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            var coreItems = new[]
            {
                new ComboBoxItem { Content = loader.GetString("v2rayWinUI.CoreType.Xray"), Tag = "Xray" },
                new ComboBoxItem { Content = loader.GetString("v2rayWinUI.CoreType.v2fly"), Tag = "v2fly" },
                new ComboBoxItem { Content = loader.GetString("v2rayWinUI.CoreType.SingBox"), Tag = "SingBox" }
            };
            cmbCoreType.ItemsSource = coreItems;
            cmbCoreType.SelectedIndex = 0;
        }
        catch
        {
            cmbCoreType.ItemsSource = new[] { "Xray", "v2fly", "SingBox" };
            cmbCoreType.SelectedIndex = 0;
        }

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
        btnCancel.Click += (s, e) => CloseWithResult(false);
    }

    private async Task SaveSettings()
    {
        if (_config == null) return;

        try
        {
            if (_config.CoreBasicItem != null)
            {
                _config.CoreBasicItem.LogEnabled = chkLogEnabled.IsChecked ?? false;
                _config.CoreBasicItem.MuxEnabled = chkMuxEnabled.IsChecked ?? false;
            }

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

            await ConfigHandler.SaveConfig(_config);

            _ = await SysProxyHandler.UpdateSysProxy(_config, false);

            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string title = loader.GetString("v2rayWinUI.Common.Success");
            string msg = loader.GetString("v2rayWinUI.Common.SettingsSaved");
            await ShowMessageAsync(title, msg);
            CloseWithResult(true);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"OptionSettingWindow SaveSettings error: {ex.Message}");
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            await ShowMessageAsync(loader.GetString("v2rayWinUI.Common.Error"), $"Failed to save settings: {ex.Message}");
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
        try
        {
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string ok = loader.GetString("v2rayWinUI.Common.OK");

            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = ok,
                XamlRoot = (this.Content as FrameworkElement)?.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch
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

    public Task<bool> ShowDialogAsync(Window? owner, int width, int height)
    {
        _closeCompletionSource = new TaskCompletionSource<bool>();
        _dialogResult = false;

        if (owner != null)
        {
            Helpers.ModalWindowHelper.ShowModal(this, owner, width, height);
        }
        else
        {
            Activate();
        }

        return _closeCompletionSource.Task;
    }

    private void CloseWithResult(bool result)
    {
        _dialogResult = result;
        Close();
    }

    private void CompleteDialogResult()
    {
        _closeCompletionSource?.TrySetResult(_dialogResult);
    }
}
