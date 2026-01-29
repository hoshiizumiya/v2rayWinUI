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
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _saveTimer;

    public RoutingSettingsPage()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        Loaded += (_, _) => Load();

        btnSave.Visibility = Visibility.Collapsed;

        cmbDomainStrategy.SelectionChanged += (_, _) => QueueSave();
        txtRoutingIndexId.TextChanged += (_, _) => QueueSave();
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
        if (_config?.RoutingBasicItem == null) return;

        cmbDomainStrategy.ItemsSource = new[]
        {
            "AsIs",
            "IPIfNonMatch",
            "IPOnDemand"
        };

        string existing = _config.RoutingBasicItem.DomainStrategy;
        cmbDomainStrategy.SelectedItem = string.IsNullOrWhiteSpace(existing) ? "AsIs" : existing;
        txtRoutingIndexId.Text = _config.RoutingBasicItem.RoutingIndexId ?? string.Empty;
    }

    private async Task SaveAsync()
    {
        if (_config?.RoutingBasicItem == null) return;

        try
        {
            string selectedStrategy = (cmbDomainStrategy.SelectedItem as string) ?? "AsIs";
            string? routingId = string.IsNullOrWhiteSpace(txtRoutingIndexId.Text) ? null : txtRoutingIndexId.Text.Trim();

            _config.RoutingBasicItem.DomainStrategy = selectedStrategy;
            _config.RoutingBasicItem.RoutingIndexId = routingId ?? string.Empty;

            _ = await ConfigHandler.SaveConfig(_config);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
