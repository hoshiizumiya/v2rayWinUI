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
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _saveTimer;

    public SystemProxySettingsPage()
    {
        InitializeComponent();
        _config = AppManager.Instance.Config;

        Loaded += (_, _) => Load();

        btnApply.Visibility = Visibility.Collapsed;

        cmbSysProxyType.SelectionChanged += (_, _) => QueueSaveAndApply();
        chkNotProxyLocal.Checked += (_, _) => QueueSaveAndApply();
        chkNotProxyLocal.Unchecked += (_, _) => QueueSaveAndApply();
        txtExceptions.TextChanged += (_, _) => QueueSaveAndApply();
        txtAdvanced.TextChanged += (_, _) => QueueSaveAndApply();
        txtPac.TextChanged += (_, _) => QueueSaveAndApply();
        txtScript.TextChanged += (_, _) => QueueSaveAndApply();

        btnDisable.Click += async (_, _) => await SaveAndApplyAsync(forceDisable: true);
    }

    private void QueueSaveAndApply()
    {
        if (_saveTimer == null)
        {
            _saveTimer = DispatcherQueue.CreateTimer();
            _saveTimer.Interval = TimeSpan.FromMilliseconds(350);
            _saveTimer.IsRepeating = false;
            _saveTimer.Tick += async (_, _) =>
            {
                await SaveAndApplyAsync(forceDisable: false);
            };
        }

        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void Load()
    {
        if (_config == null) return;

        List<ESysProxyType> sysProxyTypes = Enum.GetValues(typeof(ESysProxyType)).Cast<ESysProxyType>().ToList();
        cmbSysProxyType.ItemsSource = sysProxyTypes;
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

        _ = await ConfigHandler.SaveConfig(_config);
        await SysProxyHandler.UpdateSysProxy(_config, forceDisable);
    }
}
