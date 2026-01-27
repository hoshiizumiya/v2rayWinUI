using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using System;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class RoutingSettingsPage : Page
{
    private Config? _config;

    public RoutingSettingsPage()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        Loaded += (_, _) => Load();
        btnSave.Click += async (_, _) => await SaveAsync();
    }

    private void Load()
    {
        if (_config?.RoutingBasicItem == null) return;

        cmbDomainStrategy.ItemsSource = new[]
        {
            "AsIs",
            "IPIfNonMatch",
            "IPOnDemand"
        };

        var existing = _config.RoutingBasicItem.DomainStrategy;
        cmbDomainStrategy.SelectedItem = string.IsNullOrWhiteSpace(existing) ? "AsIs" : existing;
        txtRoutingIndexId.Text = _config.RoutingBasicItem.RoutingIndexId ?? string.Empty;
    }

    private async Task SaveAsync()
    {
        if (_config?.RoutingBasicItem == null) return;

        try
        {
            _config.RoutingBasicItem.DomainStrategy = (cmbDomainStrategy.SelectedItem as string) ?? "AsIs";
            _config.RoutingBasicItem.RoutingIndexId = string.IsNullOrWhiteSpace(txtRoutingIndexId.Text) ? null : txtRoutingIndexId.Text;

            await ConfigHandler.SaveConfig(_config);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"RoutingSettingsPage Save error: {ex.Message}");
        }
    }
}
