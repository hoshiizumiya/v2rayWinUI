using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
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
        if (_config?.RoutingBasicItem == null)
            return;

        try
        {
            ResourceLoader loader = new ResourceLoader();
            ComboBoxItem[] items = new[]
            {
                new ComboBoxItem { Tag = "AsIs", Content = loader.GetString("v2rayWinUI.Routing.DomainStrategy.AsIs") },
                new ComboBoxItem { Tag = "IPIfNonMatch", Content = loader.GetString("v2rayWinUI.Routing.DomainStrategy.IPIfNonMatch") },
                new ComboBoxItem { Tag = "IPOnDemand", Content = loader.GetString("v2rayWinUI.Routing.DomainStrategy.IPOnDemand") }
            };
            cmbDomainStrategy.ItemsSource = items;

            string existing = _config.RoutingBasicItem.DomainStrategy;
            ComboBoxItem? sel = items.FirstOrDefault(i => (i.Tag as string) == existing);
            cmbDomainStrategy.SelectedItem = sel ?? items[0];
        }
        catch
        {
            cmbDomainStrategy.ItemsSource = new[] { "AsIs", "IPIfNonMatch", "IPOnDemand" };
            string existing = _config.RoutingBasicItem.DomainStrategy;
            cmbDomainStrategy.SelectedItem = string.IsNullOrWhiteSpace(existing) ? "AsIs" : existing;
        }
        txtRoutingIndexId.Text = _config.RoutingBasicItem.RoutingIndexId ?? string.Empty;
    }

    private async Task SaveAsync()
    {
        if (_config?.RoutingBasicItem == null)
            return;

        try
        {
            string selectedStrategy = "AsIs";
            if (cmbDomainStrategy.SelectedItem is ComboBoxItem cbi && cbi.Tag is string tag)
            {
                selectedStrategy = tag;
            }
            else if (cmbDomainStrategy.SelectedItem is string s)
            {
                selectedStrategy = s;
            }
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
