using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.ViewModels;

namespace v2rayWinUI.Views.Settings.SettingsPages;

public sealed partial class FullConfigTemplateSettingsPage : Page
{
    private readonly FullConfigTemplateViewModel _viewModel;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _saveTimer;

    public FullConfigTemplateSettingsPage()
    {
        InitializeComponent();

        _viewModel = new FullConfigTemplateViewModel((App.Current as v2rayWinUI.App)?.MainWindowHandler);

        Loaded += (_, _) =>
        {
            BindFromViewModel();
        };

        FullConfigTemplateSettingsPageSaveButton.Visibility = Visibility.Collapsed;

        FullConfigTemplateSettingsPageXrayEnableToggle.Toggled += (_, _) => QueueSave();
        FullConfigTemplateSettingsPageXrayAddProxyOnlyToggle.Toggled += (_, _) => QueueSave();
        FullConfigTemplateSettingsPageXrayProxyDetourTextBox.TextChanged += (_, _) => QueueSave();
        FullConfigTemplateSettingsPageXrayTemplateTextBox.TextChanged += (_, _) => QueueSave();

        FullConfigTemplateSettingsPageSingboxEnableToggle.Toggled += (_, _) => QueueSave();
        FullConfigTemplateSettingsPageSingboxAddProxyOnlyToggle.Toggled += (_, _) => QueueSave();
        FullConfigTemplateSettingsPageSingboxProxyDetourTextBox.TextChanged += (_, _) => QueueSave();
        FullConfigTemplateSettingsPageSingboxTemplateTextBox.TextChanged += (_, _) => QueueSave();
        FullConfigTemplateSettingsPageSingboxTunTemplateTextBox.TextChanged += (_, _) => QueueSave();
    }

    private void QueueSave()
    {
        if (_saveTimer == null)
        {
            _saveTimer = DispatcherQueue.CreateTimer();
            _saveTimer.Interval = TimeSpan.FromMilliseconds(600);
            _saveTimer.IsRepeating = false;
            _saveTimer.Tick += (_, _) =>
            {
                try
                {
                    UpdateViewModelFromUI();
                    _viewModel.SaveCmd.Execute().Subscribe();
                }
                catch { }
            };
        }

        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void BindFromViewModel()
    {
        FullConfigTemplateSettingsPageXrayEnableToggle.IsOn = _viewModel.EnableFullConfigTemplate4Ray;
        FullConfigTemplateSettingsPageXrayTemplateTextBox.Text = _viewModel.FullConfigTemplate4Ray ?? string.Empty;
        FullConfigTemplateSettingsPageXrayAddProxyOnlyToggle.IsOn = _viewModel.AddProxyOnly4Ray;
        FullConfigTemplateSettingsPageXrayProxyDetourTextBox.Text = _viewModel.ProxyDetour4Ray ?? string.Empty;

        FullConfigTemplateSettingsPageSingboxEnableToggle.IsOn = _viewModel.EnableFullConfigTemplate4Singbox;
        FullConfigTemplateSettingsPageSingboxTemplateTextBox.Text = _viewModel.FullConfigTemplate4Singbox ?? string.Empty;
        FullConfigTemplateSettingsPageSingboxTunTemplateTextBox.Text = _viewModel.FullTunConfigTemplate4Singbox ?? string.Empty;
        FullConfigTemplateSettingsPageSingboxAddProxyOnlyToggle.IsOn = _viewModel.AddProxyOnly4Singbox;
        FullConfigTemplateSettingsPageSingboxProxyDetourTextBox.Text = _viewModel.ProxyDetour4Singbox ?? string.Empty;
    }

    private void UpdateViewModelFromUI()
    {
        _viewModel.EnableFullConfigTemplate4Ray = FullConfigTemplateSettingsPageXrayEnableToggle.IsOn;
        _viewModel.FullConfigTemplate4Ray = FullConfigTemplateSettingsPageXrayTemplateTextBox.Text;
        _viewModel.AddProxyOnly4Ray = FullConfigTemplateSettingsPageXrayAddProxyOnlyToggle.IsOn;
        _viewModel.ProxyDetour4Ray = FullConfigTemplateSettingsPageXrayProxyDetourTextBox.Text;

        _viewModel.EnableFullConfigTemplate4Singbox = FullConfigTemplateSettingsPageSingboxEnableToggle.IsOn;
        _viewModel.FullConfigTemplate4Singbox = FullConfigTemplateSettingsPageSingboxTemplateTextBox.Text;
        _viewModel.FullTunConfigTemplate4Singbox = FullConfigTemplateSettingsPageSingboxTunTemplateTextBox.Text;
        _viewModel.AddProxyOnly4Singbox = FullConfigTemplateSettingsPageSingboxAddProxyOnlyToggle.IsOn;
        _viewModel.ProxyDetour4Singbox = FullConfigTemplateSettingsPageSingboxProxyDetourTextBox.Text;
    }
}
