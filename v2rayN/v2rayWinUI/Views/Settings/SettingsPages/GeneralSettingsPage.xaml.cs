using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using System;
using System.Linq;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class GeneralSettingsPage : Page
{
    private Config? _config;

    public GeneralSettingsPage()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        Loaded += (_, _) => Load();
        btnSave.Click += async (_, _) => await SaveAsync();
    }

    private void Load()
    {
        if (_config == null) return;

        cmbCoreType.ItemsSource = new[] { "Xray", "v2fly", "SingBox" };
        cmbCoreType.SelectedIndex = 0;

        chkLogEnabled.IsChecked = _config.CoreBasicItem?.LogEnabled ?? false;
        chkMuxEnabled.IsChecked = _config.CoreBasicItem?.MuxEnabled ?? false;

        var socksInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "socks");
        txtSocksPort.Text = socksInbound?.LocalPort.ToString() ?? "10808";

        var httpInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "http");
        txtHttpPort.Text = httpInbound?.LocalPort.ToString() ?? "10809";

        chkAllowLANConn.IsChecked = socksInbound?.AllowLANConn ?? false;
    }

    private async Task SaveAsync()
    {
        if (_config == null) return;

        try
        {
            if (_config.CoreBasicItem != null)
            {
                _config.CoreBasicItem.LogEnabled = chkLogEnabled.IsChecked ?? false;
                _config.CoreBasicItem.MuxEnabled = chkMuxEnabled.IsChecked ?? false;
            }

            if (int.TryParse(txtSocksPort.Text, out var socksPort))
            {
                var socksInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "socks");
                if (socksInbound != null)
                {
                    socksInbound.LocalPort = socksPort;
                    socksInbound.AllowLANConn = chkAllowLANConn.IsChecked ?? false;
                }
            }

            if (int.TryParse(txtHttpPort.Text, out var httpPort))
            {
                var httpInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "http");
                if (httpInbound != null)
                {
                    httpInbound.LocalPort = httpPort;
                }
            }

            await ConfigHandler.SaveConfig(_config);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"GeneralSettingsPage Save error: {ex.Message}");
        }
    }
}
