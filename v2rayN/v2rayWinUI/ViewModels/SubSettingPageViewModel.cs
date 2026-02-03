using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLib.ViewModels;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Threading.Tasks;

namespace v2rayWinUI.ViewModels;

/// <summary>
/// WinUI-facing wrapper to provide Toolkit commands and local enablement without touching ServiceLib.
/// </summary>
public partial class SubSettingPageViewModel : ObservableObject
{
    private readonly SubSettingViewModel _serviceVm;
    private readonly Func<Exception, Task> _errorHandler;

    [ObservableProperty]
    private bool _canEditRemove;

    public IAsyncRelayCommand AddCommand { get; }
    public IAsyncRelayCommand EditCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }

    public SubSettingPageViewModel(SubSettingViewModel serviceVm, Func<Exception, Task> errorHandler)
    {
        _serviceVm = serviceVm;
        _errorHandler = errorHandler;

        AddCommand = new AsyncRelayCommand(ExecuteAddAsync);
        EditCommand = new AsyncRelayCommand(ExecuteEditAsync, CanExecuteEditRemove);
        DeleteCommand = new AsyncRelayCommand(ExecuteDeleteAsync, CanExecuteEditRemove);

        UpdateCanEditRemove();
        _serviceVm.PropertyChanged += (_, __) => UpdateCanEditRemove();
    }

    public void UpdateCanEditRemove()
    {
        CanEditRemove = _serviceVm?.SelectedSource != null && !string.IsNullOrEmpty(_serviceVm.SelectedSource?.Id);
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    private async Task ExecuteAddAsync()
    {
        await ExecuteServiceCommand(_serviceVm.SubAddCmd);
    }

    private async Task ExecuteEditAsync()
    {
        await ExecuteServiceCommand(_serviceVm.SubEditCmd);
    }

    private async Task ExecuteDeleteAsync()
    {
        await ExecuteServiceCommand(_serviceVm.SubDeleteCmd);
    }

    private async Task ExecuteServiceCommand(ReactiveCommand<Unit, Unit> cmd)
    {
        try
        {
            await cmd.Execute().ToTask();
        }
        catch (Exception ex)
        {
            await _errorHandler(ex);
        }
    }

    private bool CanExecuteEditRemove() => CanEditRemove;
}
