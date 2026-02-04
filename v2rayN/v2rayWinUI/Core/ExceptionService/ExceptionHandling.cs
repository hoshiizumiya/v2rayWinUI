// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.
// Inspired by SnapHutao's exception handling

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Sentry;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using v2rayWinUI.UI.Xaml.View.Window;
using ServiceLib.Common;

namespace v2rayWinUI.Core.ExceptionService;

internal sealed class ExceptionHandling
{
    private readonly ILogger<ExceptionHandling> logger;
    private static bool isExiting = false;

    public ExceptionHandling(IServiceProvider serviceProvider)
    {
        logger = serviceProvider.GetRequiredService<ILogger<ExceptionHandling>>();
    }

    public static void Initialize(IServiceProvider serviceProvider, Application app)
    {
        serviceProvider.GetRequiredService<ExceptionHandling>().Attach(app);
    }

    /// <summary>
    /// Kill the current process if the exception is or has a DbException.
    /// As this method does not throw, it should only be used in catch blocks
    /// </summary>
    /// <param name="exception">Incoming exception</param>
    /// <returns>Unwrapped DbException or original exception</returns>
    [StackTraceHidden]
    public static Exception KillProcessOnDbExceptionNoThrow(Exception exception)
    {
        if (exception is DbException dbException)
        {
            return KillProcessOnDbException(dbException);
        }

        if (exception.InnerException is DbException dbException2)
        {
            return KillProcessOnDbException(dbException2);
        }

        return exception;
    }

    [StackTraceHidden]
    private static DbException KillProcessOnDbException(DbException exception)
    {
        // Show error message and exit
        Environment.FailFast(exception.Message, exception);
        return exception;
    }

    private static void OnAppUnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Exception? exception = e.Exception;

        if (exception is null || isExiting)
        {
            return;
        }

        isExiting = true;
        Debugger.Break();

        KillProcessOnDbExceptionNoThrow(e.Exception);

        // Set Sentry mechanism for tracking
        exception.SetSentryMechanism("Microsoft.UI.Xaml.UnhandledException", handled: false);

        // Capture exception to Sentry with attachments
        SentryId id = SentrySdk.CaptureException(e.Exception, scope =>
        {
            if (ExceptionAttachment.TryGetAttachment(e.Exception, out SentryAttachment? attachment))
            {
                scope.AddAttachment(attachment);
            }
        });

        // Flush to ensure Sentry receives the report
        SentrySdk.Flush();

        // Mark as handled to prevent default crash behavior
        e.Handled = true;

        // Show exception report window on UI thread
        CapturedException capturedException = new(id, exception);

        if (SynchronizationContext.Current is { } syncContext)
        {
            syncContext.Post(static state =>
            {
                try
                {
                    if (state is CapturedException captured)
                    {
                        ExceptionReportWindow.Show(captured);
                    }
                }
                catch (Exception ex)
                {
                    Logging.SaveLog($"Failed to show exception window: {ex}");
                    Environment.Exit(1);
                }
            }, capturedException);
        }
        else
        {
            // Fallback if no sync context available
            Environment.Exit(1);
        }
    }

    private void Attach(Application app)
    {
        app.UnhandledException += OnAppUnhandledException;
        ConfigureDebugSettings(app);
    }

    [Conditional("DEBUG")]
    private void ConfigureDebugSettings(Application app)
    {
        app.DebugSettings.FailFastOnErrors = false;

        app.DebugSettings.IsBindingTracingEnabled = true;
        app.DebugSettings.BindingFailed += OnXamlBindingFailed;

        app.DebugSettings.IsXamlResourceReferenceTracingEnabled = true;
        app.DebugSettings.XamlResourceReferenceFailed += OnXamlResourceReferenceFailed;

        app.DebugSettings.LayoutCycleTracingLevel = LayoutCycleTracingLevel.High;
        app.DebugSettings.LayoutCycleDebugBreakLevel = LayoutCycleDebugBreakLevel.High;
    }

    private void OnXamlBindingFailed(object? sender, BindingFailedEventArgs e)
    {
        logger.LogCritical("XAML Binding Failed:{Message}", e.Message);
    }

    private void OnXamlResourceReferenceFailed(DebugSettings sender, XamlResourceReferenceFailedEventArgs e)
    {
        logger.LogCritical("XAML Resource Reference Failed:{Message}", e.Message);
    }
}
