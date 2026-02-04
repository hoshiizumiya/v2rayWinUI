// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace v2rayWinUI.UI.Xaml.ViewModel;

/// <summary>
/// Base class for all MVVM ViewModels in v2rayWinUI
/// Provides common functionality following CommunityToolkit.Mvvm patterns
/// </summary>
public abstract partial class V2rayViewModelBase : ObservableObject
{
    /// <summary>
    /// Gets a value indicating whether the view model is in a busy state
    /// </summary>
    private bool isBusy;

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    /// <summary>
    /// Gets or sets the error message to display
    /// </summary>
    private string? errorMessage;

    public string? ErrorMessage
    {
        get => errorMessage;
        set => SetProperty(ref errorMessage, value);
    }

    /// <summary>
    /// Clear the error message
    /// </summary>
    [RelayCommand]
    public void ClearError()
    {
        ErrorMessage = null;
    }

    /// <summary>
    /// Show an error message
    /// </summary>
    protected void ShowError(string message)
    {
        ErrorMessage = message;
    }

    /// <summary>
    /// Show an error from an exception
    /// </summary>
    protected void ShowError(Exception exception)
    {
        ShowError(exception.Message);
    }
}
