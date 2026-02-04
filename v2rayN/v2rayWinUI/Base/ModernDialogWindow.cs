// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Common;
using System;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

namespace v2rayWinUI.Base;

/// <summary>
/// Base class for modern dialog windows with WinUI 3 styling
/// Provides ExtendIntoTitleBar, custom title bar, resizability, and proper modal behavior
/// Inspired by SnapHutao's window architecture
/// </summary>
public abstract class ModernDialogWindow : Window
{
    protected new AppWindow? AppWindow { get; private set; }
    protected Grid? TitleBarContainer { get; set; }
    protected TextBlock? TitleTextBlock { get; set; }
    
    private TaskCompletionSource<bool>? _closeCompletionSource;
    protected bool DialogResult { get; set; }

    protected ModernDialogWindow()
    {
        InitializeAppWindow();
        ConfigureTitleBar();
        
        Closed += (_, _) => CompleteDialogResult();
    }

    private void InitializeAppWindow()
    {
        try
        {
            nint hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        }
        catch (Exception ex)
        {
            Logging.SaveLog($"ModernDialogWindow.InitializeAppWindow failed: {ex.Message}");
        }
    }

    private void ConfigureTitleBar()
    {
        try
        {
            if (AppWindow?.TitleBar != null && AppWindowTitleBar.IsCustomizationSupported())
            {
                AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
                AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"ModernDialogWindow.ConfigureTitleBar failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the custom title bar element
    /// Call this in derived class constructor after InitializeComponent
    /// </summary>
    protected new void SetTitleBar(UIElement titleBarElement)
    {
        try
        {
            if (AppWindow?.TitleBar != null && AppWindowTitleBar.IsCustomizationSupported())
            {
                AppWindow.TitleBar.SetDragRectangles(new RectInt32[] { });
            }

            if (titleBarElement is FrameworkElement element)
            {
                element.Loaded += (_, _) =>
                {
                    try
                    {
                        SetDragRegionForCustomTitleBar(element);
                    }
                    catch { }
                };
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"ModernDialogWindow.SetTitleBar failed: {ex.Message}");
        }
    }

    private void SetDragRegionForCustomTitleBar(FrameworkElement titleBar)
    {
        try
        {
            if (AppWindow?.TitleBar == null || !AppWindowTitleBar.IsCustomizationSupported())
                return;

            double scale = (Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1.0;
            
            RectInt32 dragRect = new()
            {
                X = 0,
                Y = 0,
                Width = (int)(titleBar.ActualWidth * scale),
                Height = (int)(titleBar.ActualHeight * scale)
            };

            AppWindow.TitleBar.SetDragRectangles(new[] { dragRect });
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"ModernDialogWindow.SetDragRegionForCustomTitleBar failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Show this window as a modal dialog
    /// Returns true if user accepted, false if cancelled
    /// </summary>
    public Task<bool> ShowDialogAsync(Window? owner, int width, int height)
    {
        _closeCompletionSource = new TaskCompletionSource<bool>();
        DialogResult = false;

        try
        {
            if (AppWindow != null)
            {
                AppWindow.Resize(new SizeInt32(width, height));
            }

            if (owner != null)
            {
                Helpers.ModalWindowHelper.ShowModal(this, owner, width, height);
            }
            else
            {
                Activate();
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"ModernDialogWindow.ShowDialogAsync failed: {ex.Message}");
            _closeCompletionSource.TrySetResult(false);
        }

        return _closeCompletionSource.Task;
    }

    /// <summary>
    /// Close the window with a result
    /// </summary>
    protected void CloseWithResult(bool result)
    {
        DialogResult = result;
        Close();
    }

    private void CompleteDialogResult()
    {
        _closeCompletionSource?.TrySetResult(DialogResult);
    }

    /// <summary>
    /// Create a standard title bar for the dialog
    /// Call this from derived class to set up title bar UI
    /// </summary>
    protected Grid CreateStandardTitleBar(string title)
    {
        Grid titleBar = new Grid
        {
            Height = 40,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Transparent)
        };

        TextBlock titleText = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
            Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style
        };

        titleBar.Children.Add(titleText);
        TitleTextBlock = titleText;
        TitleBarContainer = titleBar;

        return titleBar;
    }
}
