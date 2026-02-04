using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using ReactiveUI;
using Sentry;
using v2rayWinUI.Core.ExceptionService;
using v2rayWinUI.UI.Xaml.View.Window;

namespace v2rayWinUI.Helpers;

/// <summary>
/// Global exception handler for ReactiveUI observables to prevent app crashes
/// Inspired by SnapHutao's error handling approach
/// </summary>
public static class ObservableExceptionHandler
{
    private static List<Exception> _capturedExceptions = new();
    public static event EventHandler<Exception>? ExceptionCaptured;
    private static bool _isShowingException = false;

    public static void Initialize()
    {
        // Global exception handler for ReactiveUI observable errors
        // This handles unhandled exceptions in reactive pipelines
        RxApp.DefaultExceptionHandler = new ReactiveExceptionHandler();
    }

    /// <summary>
    /// Custom exception handler for ReactiveUI
    /// </summary>
    private class ReactiveExceptionHandler : IObserver<Exception>
    {
        public void OnNext(Exception value)
        {
            try
            {
                HandleException(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in exception handler: {ex}");
            }
        }

        public void OnError(Exception error)
        {
            try
            {
                HandleException(error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in exception handler: {ex}");
            }
        }

        public void OnCompleted()
        {
            // Nothing to do on completion
        }
    }

    /// <summary>
    /// Centralized exception handling with error reporting
    /// </summary>
    public static void HandleException(Exception exception)
    {
        if (exception == null) return;

        // Log the exception
        var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception.GetType().Name}: {exception.Message}";
        System.Diagnostics.Debug.WriteLine(message);

        // Store for analysis
        _capturedExceptions.Add(exception);
        if (_capturedExceptions.Count > 100)
        {
            _capturedExceptions.RemoveAt(0);
        }

        // Capture to Sentry
        try
        {
            exception.SetSentryMechanism("ReactiveUI.Pipeline", handled: true);
            SentryId id = SentrySdk.CaptureException(exception);
            SentrySdk.Flush();

            // Show exception window if not already showing one
            if (!_isShowingException)
            {
                _isShowingException = true;
                try
                {
                    var syncContext = SynchronizationContext.Current;
                    if (syncContext != null)
                    {
                        syncContext.Post(_ =>
                        {
                            try
                            {
                                ExceptionReportWindow.Show(new CapturedException(id, exception));
                            }
                            finally
                            {
                                _isShowingException = false;
                            }
                        }, null);
                    }
                }
                catch
                {
                    _isShowingException = false;
                }
            }
        }
        catch
        {
            // Silently fail if Sentry/window show fails
        }

        // Raise event for exception listeners
        ExceptionCaptured?.Invoke(null, exception);
    }

    /// <summary>
    /// Wrap an observable with exception handling
    /// </summary>
    public static IObservable<T> Catch<T>(
        this IObservable<T> source,
        Action<Exception>? onError = null,
        string? operationName = null)
    {
        return source
            .Catch<T, Exception>(ex =>
            {
                HandleException(ex);
                onError?.Invoke(ex);
                return Observable.Empty<T>();
            });
    }

    /// <summary>
    /// Subscribe with automatic exception handling
    /// </summary>
    public static IDisposable SafeSubscribe<T>(
        this IObservable<T> source,
        Action<T>? onNext = null,
        Action<Exception>? onError = null,
        Action? onCompleted = null,
        string? operationName = null)
    {
        return source
            .Catch<T>(
                ex => onError?.Invoke(ex),
                operationName)
            .Subscribe(
                t =>
                {
                    try
                    {
                        onNext?.Invoke(t);
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex);
                        onError?.Invoke(ex);
                    }
                },
                ex =>
                {
                    HandleException(ex);
                    onError?.Invoke(ex);
                },
                () =>
                {
                    try
                    {
                        onCompleted?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex);
                    }
                });
    }

    /// <summary>
    /// Get captured exceptions for debugging
    /// </summary>
    public static IReadOnlyList<Exception> GetCapturedExceptions()
    {
        return _capturedExceptions.AsReadOnly();
    }

    /// <summary>
    /// Clear captured exceptions
    /// </summary>
    public static void ClearCapturedExceptions()
    {
        _capturedExceptions.Clear();
    }
}
