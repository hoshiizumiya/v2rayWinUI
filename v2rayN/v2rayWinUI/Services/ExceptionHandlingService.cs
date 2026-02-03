// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;

namespace v2rayWinUI.Services;

internal sealed class ExceptionHandlingService
{
    private readonly Application app;
    private readonly IExceptionReporter exceptionReporter;

    public ExceptionHandlingService(Application app, IExceptionReporter exceptionReporter)
    {
        this.app = app;
        this.exceptionReporter = exceptionReporter;
    }

    public void Initialize()
    {
        app.UnhandledException += OnAppUnhandledException;

#if DEBUG
        ConfigureDebugSettings();
#endif
    }

    private void OnAppUnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Exception? exception = e.Exception;

        if (exception is null)
        {
            return;
        }

        try
        {
            exception.Data["Handled"] = false;
            exceptionReporter.Report(exception, "UnhandledException");
        }
        catch { }

        e.Handled = true;
    }

    [Conditional("DEBUG")]
    private void ConfigureDebugSettings()
    {
        app.DebugSettings.FailFastOnErrors = false;
        app.DebugSettings.IsBindingTracingEnabled = true;
        app.DebugSettings.BindingFailed += OnXamlBindingFailed;
    }

    private void OnXamlBindingFailed(object? sender, BindingFailedEventArgs e)
    {
        try
        {
            Exception bindingException = new InvalidOperationException($"XAML Binding Failed: {e.Message}");
            exceptionReporter.Report(bindingException, "XamlBindingFailed");
        }
        catch { }
    }
}
