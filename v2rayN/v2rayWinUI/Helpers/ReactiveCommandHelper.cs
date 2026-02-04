using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace v2rayWinUI.Helpers;

/// <summary>
/// Helper class to safely execute ReactiveUI commands with proper null checks and exception handling
/// </summary>
public static class ReactiveCommandHelper
{
    private static ILogger? _logger;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Safely execute a ReactiveCommand with automatic exception handling
    /// </summary>
    public static IDisposable SafeExecute<T>(
        this ReactiveCommand<uint, T>? command,
        Action<T>? onSuccess = null,
        Action<Exception>? onError = null,
        string? operationName = null)
    {
        if (command == null)
        {
            LogWarning($"Attempted to execute null command: {operationName}");
            return Disposable.Empty;
        }

        try
        {
            return command.Execute()
                .Subscribe(
                    result =>
                    {
                        try
                        {
                            onSuccess?.Invoke(result);
                            LogDebug($"Command executed successfully: {operationName}");
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error in success handler for {operationName}: {ex.Message}", ex);
                            onError?.Invoke(ex);
                        }
                    },
                    error =>
                    {
                        LogError($"Command execution failed: {operationName} - {error.Message}", error);
                        onError?.Invoke(error);
                    },
                    () =>
                    {
                        LogDebug($"Command completed: {operationName}");
                    });
        }
        catch (Exception ex)
        {
            LogError($"Exception during command execution: {operationName} - {ex.Message}", ex);
            onError?.Invoke(ex);
            return Disposable.Empty;
        }
    }

    /// <summary>
    /// Safely execute a ReactiveCommand with a parameter
    /// </summary>
    public static IDisposable SafeExecute<TParam, TResult>(
        this ReactiveCommand<TParam, TResult>? command,
        TParam parameter,
        Action<TResult>? onSuccess = null,
        Action<Exception>? onError = null,
        string? operationName = null)
    {
        if (command == null)
        {
            LogWarning($"Attempted to execute null command with parameter: {operationName}");
            return Disposable.Empty;
        }

        try
        {
            return command.Execute(parameter)
                .Subscribe(
                    result =>
                    {
                        try
                        {
                            onSuccess?.Invoke(result);
                            LogDebug($"Command executed successfully: {operationName}");
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error in success handler for {operationName}: {ex.Message}", ex);
                            onError?.Invoke(ex);
                        }
                    },
                    error =>
                    {
                        LogError($"Command execution failed: {operationName} - {error.Message}", error);
                        onError?.Invoke(error);
                    },
                    () =>
                    {
                        LogDebug($"Command completed: {operationName}");
                    });
        }
        catch (Exception ex)
        {
            LogError($"Exception during command execution: {operationName} - {ex.Message}", ex);
            onError?.Invoke(ex);
            return Disposable.Empty;
        }
    }

    /// <summary>
    /// Safely execute an async ReactiveCommand
    /// </summary>
    public static async Task SafeExecuteAsync<T>(
        this ReactiveCommand<uint, T>? command,
        Action<T>? onSuccess = null,
        Action<Exception>? onError = null,
        string? operationName = null)
    {
        if (command == null)
        {
            LogWarning($"Attempted to execute null async command: {operationName}");
            return;
        }

        try
        {
            var result = await command.Execute();
            onSuccess?.Invoke(result);
            LogDebug($"Async command executed successfully: {operationName}");
        }
        catch (Exception ex)
        {
            LogError($"Async command execution failed: {operationName} - {ex.Message}", ex);
            onError?.Invoke(ex);
        }
    }

    private static void LogDebug(string message)
    {
        _logger?.LogDebug(message);
    }

    private static void LogWarning(string message)
    {
        _logger?.LogWarning(message);
    }

    private static void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
            _logger?.LogError(ex, message);
        else
            _logger?.LogError(message);
    }
}
