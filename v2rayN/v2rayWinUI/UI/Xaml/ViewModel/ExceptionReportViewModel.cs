// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentry;
using System.Collections.ObjectModel;

namespace v2rayWinUI.UI.Xaml.View.Window;

/// <summary>
/// ViewModel for exception reporting window
/// </summary>
public sealed partial class ExceptionReportViewModel : ObservableObject
{
    [ObservableProperty]
    private string exceptionMessage = string.Empty;

    [ObservableProperty]
    private string stackTrace = string.Empty;

    [ObservableProperty]
    private string traceId = string.Empty;

    [ObservableProperty]
    private string? userComment;

    [ObservableProperty]
    private bool isSendingReport;

    private readonly SentryId associatedEventId;
    private readonly Exception capturedException;

    public ExceptionReportViewModel(SentryId eventId, Exception exception)
    {
        associatedEventId = eventId;
        capturedException = exception;

        ExceptionMessage = exception.Message;
        StackTrace = exception.ToString();
        TraceId = $"trace.id: {eventId}";
    }

    [RelayCommand]
    private async Task SendReport()
    {
        IsSendingReport = true;
        try
        {
            // Send user feedback along with the event
            if (!string.IsNullOrWhiteSpace(UserComment))
            {
                // Configure scope with user feedback as extra context
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.Contexts["user_feedback"] = new Dictionary<string, object>
                    {
                        { "comment", UserComment },
                        { "event_id", associatedEventId.ToString() }
                    };
                });
            }

            // Flush to ensure all events are sent
            await SentrySdk.FlushAsync();
        }
        finally
        {
            IsSendingReport = false;
        }
    }
}
