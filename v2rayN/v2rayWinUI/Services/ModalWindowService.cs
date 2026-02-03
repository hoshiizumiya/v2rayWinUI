// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using v2rayWinUI.Helpers;

namespace v2rayWinUI.Services;

internal interface IModalWindowService
{
    Task<bool> ShowModalAsync<TWindow>(Window owner, int width, int height) where TWindow : Window, new();
    Task<bool> ShowModalAsync<TWindow>(Window owner, object parameter, int width, int height) where TWindow : Window;
}

internal sealed class ModalWindowService : IModalWindowService
{
    public Task<bool> ShowModalAsync<TWindow>(Window owner, int width, int height) where TWindow : Window, new()
    {
        TWindow window = new TWindow();
        return ShowModalInternalAsync(window, owner, width, height);
    }

    public Task<bool> ShowModalAsync<TWindow>(Window owner, object parameter, int width, int height) where TWindow : Window
    {
        TWindow window = (TWindow)Activator.CreateInstance(typeof(TWindow), parameter)!;
        return ShowModalInternalAsync(window, owner, width, height);
    }

    private Task<bool> ShowModalInternalAsync(Window window, Window owner, int width, int height)
    {
        if (window is IDialogWindow dialogWindow)
        {
            return dialogWindow.ShowDialogAsync(owner, width, height);
        }

        ModalWindowHelper.ShowModal(window, owner, width, height);
        return Task.FromResult(true);
    }
}

internal interface IDialogWindow
{
    Task<bool> ShowDialogAsync(Window? owner, int width, int height);
}
