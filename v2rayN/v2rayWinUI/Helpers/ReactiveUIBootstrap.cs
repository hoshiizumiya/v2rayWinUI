namespace v2rayWinUI.Helpers;

internal static class ReactiveUIBootstrap
{
    internal static void Initialize()
    {
        try
        {
            // Ensure ReactiveUI never uses the default handler that throws UnhandledErrorException.
            ObservableExceptionHandler.Initialize();
        }
        catch
        {
        }
    }
}
