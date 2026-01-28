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
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _saveTimer;

    public DnsSettingsPage()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        Loaded += (_, _) => Load();

        btnSave.Visibility = Visibility.Collapsed;

        chkUseSystemHosts.Checked += (_, _) => QueueSave();
        chkUseSystemHosts.Unchecked += (_, _) => QueueSave();
        txtDirectDNS.TextChanged += (_, _) => QueueSave();
        txtRemoteDNS.TextChanged += (_, _) => QueueSave();
        txtBootstrapDNS.TextChanged += (_, _) => QueueSave();
    }

    private void QueueSave()
    {
        if (_saveTimer == null)
        {
            _saveTimer = DispatcherQueue.CreateTimer();
            _saveTimer.Interval = TimeSpan.FromMilliseconds(350);
            _saveTimer.IsRepeating = false;
            _saveTimer.Tick += async (_, _) =>
            {
                await SaveAsync();
            };
        }

        _saveTimer.Stop();
        _saveTimer.Start();
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
            string? directDns = string.IsNullOrWhiteSpace(txtDirectDNS.Text) ? null : txtDirectDNS.Text.Trim();
            string? remoteDns = string.IsNullOrWhiteSpace(txtRemoteDNS.Text) ? null : txtRemoteDNS.Text.Trim();
            string? bootstrapDns = string.IsNullOrWhiteSpace(txtBootstrapDNS.Text) ? null : txtBootstrapDNS.Text.Trim();

            _config.SimpleDNSItem.DirectDNS = directDns;
            _config.SimpleDNSItem.RemoteDNS = remoteDns;
            _config.SimpleDNSItem.BootstrapDNS = bootstrapDns;

            _ = await ConfigHandler.SaveConfig(_config);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
