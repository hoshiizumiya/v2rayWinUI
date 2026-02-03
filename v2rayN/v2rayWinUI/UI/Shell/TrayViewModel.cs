using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLib.Events;
using ServiceLib.Enums;
using System;
using System.Windows.Input;

namespace v2rayWinUI.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    // Exposed as delegate so view can subscribe/unsubscribe and ViewModel can invoke to request hiding the flyout
    public Action? RequestHide;

    public TrayViewModel()
    {
        ShowCommand = new RelayCommand(ExecuteShow);
        UpdateSubsCommand = new RelayCommand(ExecuteUpdateSubs);
        ExitCommand = new RelayCommand(ExecuteExit);
        ProxyChangeCommand = new RelayCommand<string>(ExecuteProxyChange);
    }

    public ICommand ShowCommand { get; }
    public ICommand UpdateSubsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ProxyChangeCommand { get; }

    private void ExecuteShow()
    {
        try { AppEvents.ShowHideWindowRequested.Publish(true); } catch { }
        RequestHide?.Invoke();
    }

    private void ExecuteUpdateSubs()
    {
        try { AppEvents.SubscriptionsUpdateRequested.Publish(false); } catch { }
        RequestHide?.Invoke();
    }

    private void ExecuteExit()
    {
        try { AppEvents.AppExitRequested.Publish(); } catch { }
        RequestHide?.Invoke();
    }

    private void ExecuteProxyChange(string? modeText)
    {
        try
        {
            int mode = 2;
            if (!string.IsNullOrWhiteSpace(modeText))
            {
                int.TryParse(modeText, out mode);
            }

            ESysProxyType type = mode switch
            {
                0 => ESysProxyType.ForcedClear,
                1 => ESysProxyType.ForcedChange,
                2 => ESysProxyType.Unchanged,
                3 => ESysProxyType.Pac,
                _ => ESysProxyType.Unchanged
            };
            AppEvents.SysProxyChangeRequested.Publish(type);
        }
        catch { }
        RequestHide?.Invoke();
    }
}
