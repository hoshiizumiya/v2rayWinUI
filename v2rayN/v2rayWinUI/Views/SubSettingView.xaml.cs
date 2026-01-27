using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Models;
using ServiceLib.ViewModels;

namespace v2rayWinUI.Views;

public sealed partial class SubSettingView : UserControl
{
    private SubSettingViewModel? _viewModel;

    public SubSettingView()
    {
        InitializeComponent();
        Loaded += SubSettingView_Loaded;
    }

    private void SubSettingView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
        {
            _viewModel = new SubSettingViewModel(UpdateViewHandler);
            SubSettingViewList.ItemsSource = _viewModel.SubItems;

            SubSettingViewAddButton.Click += (_, _) => _viewModel?.SubAddCmd.Execute().Subscribe();
            SubSettingViewEditButton.Click += (_, _) => _viewModel?.SubEditCmd.Execute().Subscribe();
            SubSettingViewDeleteButton.Click += (_, _) => _viewModel?.SubDeleteCmd.Execute().Subscribe();

            SubSettingViewList.SelectionChanged += (_, _) =>
            {
                if (_viewModel == null) return;

                _viewModel.SelectedSources = SubSettingViewList.SelectedItems.Cast<SubItem>().ToList();
                _viewModel.SelectedSource = (SubItem?)SubSettingViewList.SelectedItem ?? new SubItem();
            };
        }
    }

    private Task<bool> UpdateViewHandler(ServiceLib.Enums.EViewAction action, object? obj)
    {
        // Delegate to MainWindow handler if possible.
        // SubSettingViewModel expects SubEditWindow, ShowYesNo, ShareSub.
        return ((App.Current as v2rayWinUI.App)?.MainWindowHandler?.Invoke(action, obj)) ?? Task.FromResult(false);
    }
}
