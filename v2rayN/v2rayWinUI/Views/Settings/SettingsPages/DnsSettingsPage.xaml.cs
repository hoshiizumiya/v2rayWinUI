using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using System;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class DnsSettingsPage : Page
{
    private Config? _config;

    public DnsSettingsPage()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        Loaded += (_, _) => Load();
        btnSave.Click += async (_, _) => await SaveAsync();
    }

    private void Load()
    {
        if (_config?.SimpleDNSItem == null) return;

        chkUseSystemHosts.IsChecked = _config.SimpleDNSItem.UseSystemHosts ?? false;
        txtDirectDNS.Text = _config.SimpleDNSItem.DirectDNS ?? string.Empty;
        txtRemoteDNS.Text = _config.SimpleDNSItem.RemoteDNS ?? string.Empty;
        txtBootstrapDNS.Text = _config.SimpleDNSItem.BootstrapDNS ?? string.Empty;
    }

    private async Task SaveAsync()
    {
        if (_config?.SimpleDNSItem == null) return;

        try
        {
            _config.SimpleDNSItem.UseSystemHosts = chkUseSystemHosts.IsChecked ?? false;
            _config.SimpleDNSItem.DirectDNS = string.IsNullOrWhiteSpace(txtDirectDNS.Text) ? null : txtDirectDNS.Text;
            _config.SimpleDNSItem.RemoteDNS = string.IsNullOrWhiteSpace(txtRemoteDNS.Text) ? null : txtRemoteDNS.Text;
            _config.SimpleDNSItem.BootstrapDNS = string.IsNullOrWhiteSpace(txtBootstrapDNS.Text) ? null : txtBootstrapDNS.Text;

            await ConfigHandler.SaveConfig(_config);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"DnsSettingsPage Save error: {ex.Message}");
        }
    }
}
