using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Enums;
using ServiceLib.Handler;
using ServiceLib.Handler.SysProxy;
using ServiceLib.Manager;
using ServiceLib.Models;
using System.Linq;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class SystemProxySettingsPage : Page
{
    private Config? _config;

    public SystemProxySettingsPage()
    {
        InitializeComponent();
        _config = AppManager.Instance.Config;

        Loaded += (_, _) => Load();

        btnApply.Click += async (_, _) => await SaveAndApplyAsync(forceDisable: false);
        btnDisable.Click += async (_, _) => await SaveAndApplyAsync(forceDisable: true);
    }

    private void Load()
    {
        if (_config == null) return;

        cmbSysProxyType.ItemsSource = Enum.GetValues(typeof(ESysProxyType)).Cast<ESysProxyType>().ToList();
        cmbSysProxyType.SelectedItem = _config.SystemProxyItem.SysProxyType;

        chkNotProxyLocal.IsChecked = _config.SystemProxyItem.NotProxyLocalAddress;
        txtExceptions.Text = _config.SystemProxyItem.SystemProxyExceptions ?? string.Empty;
        txtAdvanced.Text = _config.SystemProxyItem.SystemProxyAdvancedProtocol ?? string.Empty;
        txtPac.Text = _config.SystemProxyItem.CustomSystemProxyPacPath ?? string.Empty;
        txtScript.Text = _config.SystemProxyItem.CustomSystemProxyScriptPath ?? string.Empty;
    }

    private async Task SaveAndApplyAsync(bool forceDisable)
    {
        if (_config == null) return;

        if (cmbSysProxyType.SelectedItem is ESysProxyType type)
        {
            _config.SystemProxyItem.SysProxyType = type;
        }

        _config.SystemProxyItem.NotProxyLocalAddress = chkNotProxyLocal.IsChecked ?? true;
        _config.SystemProxyItem.SystemProxyExceptions = txtExceptions.Text ?? string.Empty;
        _config.SystemProxyItem.SystemProxyAdvancedProtocol = txtAdvanced.Text ?? string.Empty;
        _config.SystemProxyItem.CustomSystemProxyPacPath = string.IsNullOrWhiteSpace(txtPac.Text) ? null : txtPac.Text;
        _config.SystemProxyItem.CustomSystemProxyScriptPath = string.IsNullOrWhiteSpace(txtScript.Text) ? null : txtScript.Text;

        await ConfigHandler.SaveConfig(_config);
        await SysProxyHandler.UpdateSysProxy(_config, forceDisable);
    }
}
