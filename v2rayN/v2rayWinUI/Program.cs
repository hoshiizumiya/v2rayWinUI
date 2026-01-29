using Microsoft.UI.Xaml;
using System;

namespace v2rayWinUI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if !DISABLE_XAML_GENERATED_MAIN
        // If generated entry point is enabled, defer to WinUI generated main.
        // Keeping this path to allow reverting if needed.
        global::Microsoft.UI.Xaml.Application.Start(_ => new App());
#else
        global::Microsoft.UI.Xaml.Application.Start(_ => new App());
#endif
    }
}
