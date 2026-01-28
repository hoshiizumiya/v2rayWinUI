using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using WinRT;

namespace v2rayWinUI.Views.Settings;

public sealed partial class SettingsView : UserControl
{
    private readonly ObservableCollection<string> _breadcrumbItems = new();
    private bool _isInitialized;

    public SettingsView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            EnsureInitialized();
        };
    }

    public void ForceInitialize()
    {
        EnsureInitialized(forceNavigate: true);
    }

    public void NavigateToSection(string tag)
    {
        EnsureInitialized(forceNavigate: true);

        NavigationViewItem? target = SettingsNavView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(i => (i.Tag as string) == tag);
        if (target != null)
        {
            SettingsNavView.SelectedItem = target;
        }
        else
        {
            // Fallback: navigate directly.
            Type pageType = tag switch
            {
                "systemProxy" => typeof(SettingsPages.SystemProxySettingsPage),
                "routing" => typeof(SettingsPages.RoutingSettingsPage),
                "dns" => typeof(SettingsPages.DnsSettingsPage),
                "hotkeys" => typeof(SettingsPages.HotkeySettingsPage),
                "fullConfigTemplate" => typeof(SettingsPages.FullConfigTemplateSettingsPage),
                "updates" => typeof(SettingsPages.UpdateSettingsPage),
                _ => typeof(SettingsPages.GeneralSettingsPage),
            };
            UpdateBreadcrumb(tag);
            contentFrame.Navigate(pageType);
        }
    }

    private void EnsureInitialized(bool forceNavigate = false)
    {
        if (_isInitialized && !forceNavigate)
            return;

        SettingsBreadcrumbBar.ItemsSource = _breadcrumbItems;
        _breadcrumbItems.Clear();
        _breadcrumbItems.Add("Settings");

        // Setting SelectedItem triggers SelectionChanged; if already selected, force a navigate.
        SettingsNavView.SelectedItem = GeneralNavItem;
        if (_isInitialized)
        {
            contentFrame.Navigate(typeof(SettingsPages.GeneralSettingsPage));
        }

        _isInitialized = true;
    }

    private void SettingsNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        string? tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;

        Type pageType = tag switch
        {
            "systemProxy" => typeof(SettingsPages.SystemProxySettingsPage),
            "routing" => typeof(SettingsPages.RoutingSettingsPage),
            "dns" => typeof(SettingsPages.DnsSettingsPage),
            "hotkeys" => typeof(SettingsPages.HotkeySettingsPage),
            "fullConfigTemplate" => typeof(SettingsPages.FullConfigTemplateSettingsPage),
            "updates" => typeof(SettingsPages.UpdateSettingsPage),
            _ => typeof(SettingsPages.GeneralSettingsPage),
        };

        UpdateBreadcrumb(tag);
        contentFrame.Navigate(pageType);
    }

    private void UpdateBreadcrumb(string? tag)
    {
        _breadcrumbItems.Clear();
        _breadcrumbItems.Add("Settings");

        string? section = tag switch
        {
            "systemProxy" => "System Proxy",
            "routing" => "Routing",
            "dns" => "DNS",
            "hotkeys" => "Hotkeys",
            "fullConfigTemplate" => "Config Template",
            "updates" => "Updates",
            "general" or null or "" => null,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(section))
        {
            _breadcrumbItems.Add(section);
        }
    }
}
