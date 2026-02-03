// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using v2rayWinUI.Core.Threading;

namespace v2rayWinUI.Services;

internal interface IDialogService
{
    public Task<bool> ShowConfirmAsync(string title, string content);
    public Task ShowMessageAsync(string title, string content);
    public Task<ContentDialogResult> ShowDialogAsync(string title, object content, string primaryText = "", string secondaryText = "", string closeText = "OK");
}

internal sealed class DialogService : IDialogService
{
    private readonly Func<XamlRoot?> xamlRootProvider;
    private readonly ITaskContext? taskContext;

    public DialogService(Func<XamlRoot?> xamlRootProvider, ITaskContext? taskContext = null)
    {
        this.xamlRootProvider = xamlRootProvider;
        this.taskContext = taskContext;
    }

    public async Task<bool> ShowConfirmAsync(string title, string content)
    {
        XamlRoot? root = xamlRootProvider();
        if (root == null)
        {
            return false;
        }

        // Read localized strings on the UI thread
        TaskCompletionSource<(string Yes, string No)> tcs = new TaskCompletionSource<(string, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool enqueued = false;

        if (taskContext is not null)
        {
            enqueued = true;
            try
            {
                taskContext.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        ResourceLoader loader = new ResourceLoader();
                        string yes = loader.GetString("v2rayWinUI.Common.Yes");
                        string no = loader.GetString("v2rayWinUI.Common.No");
                        tcs.SetResult((yes, no));
                    }
                    catch
                    {
                        tcs.SetResult((string.Empty, string.Empty));
                    }
                });
            }
            catch
            {
                enqueued = false;
            }
        }
        else
        {
            var dq = DispatcherQueue.GetForCurrentThread();
            enqueued = dq?.TryEnqueue(() =>
            {
                try
                {
                    ResourceLoader loader = new ResourceLoader();
                    string yes = loader.GetString("v2rayWinUI.Common.Yes");
                    string no = loader.GetString("v2rayWinUI.Common.No");
                    tcs.SetResult((yes, no));
                }
                catch
                {
                    tcs.SetResult((string.Empty, string.Empty));
                }
            }) ?? false;
        }

        (string Yes, string No) resultPair;
        if (enqueued)
        {
            resultPair = await tcs.Task;
        }
        else
        {
            // Fallback if dispatcher could not enqueue
            try
            {
                ResourceLoader loader = new ResourceLoader();
                resultPair = (loader.GetString("v2rayWinUI.Common.Yes"), loader.GetString("v2rayWinUI.Common.No"));
            }
            catch
            {
                resultPair = (string.Empty, string.Empty);
            }
        }

        string yesText = resultPair.Yes;
        string noText = resultPair.No;

        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = yesText,
            CloseButtonText = noText,
            XamlRoot = root
        };

        ContentDialogResult dialogResult = await dialog.ShowAsync();
        return dialogResult == ContentDialogResult.Primary;
    }

    public async Task ShowMessageAsync(string title, string content)
    {
        XamlRoot? root = xamlRootProvider();
        if (root == null)
        {
            return;
        }

        // Read OK string on the UI thread
        TaskCompletionSource<string> tcsOk = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool enqueuedOk = false;

        if (taskContext is not null)
        {
            enqueuedOk = true;
            try
            {
                taskContext.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        ResourceLoader loader = new ResourceLoader();
                        string ok = loader.GetString("v2rayWinUI.Common.OK");
                        tcsOk.SetResult(ok);
                    }
                    catch
                    {
                        tcsOk.SetResult(string.Empty);
                    }
                });
            }
            catch
            {
                enqueuedOk = false;
            }
        }
        else
        {
            var dq = DispatcherQueue.GetForCurrentThread();
            enqueuedOk = dq?.TryEnqueue(() =>
            {
                try
                {
                    ResourceLoader loader = new ResourceLoader();
                    string ok = loader.GetString("v2rayWinUI.Common.OK");
                    tcsOk.SetResult(ok);
                }
                catch
                {
                    tcsOk.SetResult(string.Empty);
                }
            }) ?? false;
        }

        string okText;
        if (enqueuedOk)
        {
            okText = await tcsOk.Task;
        }
        else
        {
            try
            {
                ResourceLoader loader = new ResourceLoader();
                okText = loader.GetString("v2rayWinUI.Common.OK");
            }
            catch
            {
                okText = string.Empty;
            }
        }

        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = okText,
            XamlRoot = root
        };

        await dialog.ShowAsync();
    }

    public async Task<ContentDialogResult> ShowDialogAsync(string title, object content, string primaryText = "", string secondaryText = "", string closeText = "OK")
    {
        XamlRoot? root = xamlRootProvider();
        if (root == null)
        {
            return ContentDialogResult.None;
        }

        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            XamlRoot = root
        };

        if (!string.IsNullOrEmpty(primaryText))
        {
            dialog.PrimaryButtonText = primaryText;
        }
        if (!string.IsNullOrEmpty(secondaryText))
        {
            dialog.SecondaryButtonText = secondaryText;
        }
        if (!string.IsNullOrEmpty(closeText))
        {
            dialog.CloseButtonText = closeText;
        }

        return await dialog.ShowAsync();
    }
}
