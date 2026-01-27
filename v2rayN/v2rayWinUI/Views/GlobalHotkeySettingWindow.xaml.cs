using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ServiceLib.Enums;
using ServiceLib.Models;
using ServiceLib.ViewModels;

namespace v2rayWinUI.Views;

public sealed partial class GlobalHotkeySettingWindow : Window
{
    private readonly GlobalHotkeySettingViewModel _viewModel;
    private readonly List<TextBox> _textBoxes;

    public GlobalHotkeySettingWindow(Func<EViewAction, object?, Task<bool>>? updateView)
    {
        InitializeComponent();

        _viewModel = new GlobalHotkeySettingViewModel(updateView);
        _textBoxes = new() { txtGlobalHotkey0, txtGlobalHotkey1, txtGlobalHotkey2, txtGlobalHotkey3, txtGlobalHotkey4 };

        InitializeWindow();
        InitHandlers();
        BindData();
    }

    private void InitializeWindow()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 720, Height = 520 });
    }

    private void InitHandlers()
    {
        for (var i = 0; i < _textBoxes.Count; i++)
        {
            var tb = _textBoxes[i];
            tb.Tag = (EGlobalHotkey)i;
            tb.KeyDown += TxtGlobalHotkey_KeyDown;
        }

        btnReset.Click += (s, e) =>
        {
            _viewModel.ResetKeyEventItem();
            BindData();
        };

        btnSave.Click += (s, e) => _viewModel.SaveCmd.Execute().Subscribe();
        btnCancel.Click += (s, e) => Close();
    }

    private void TxtGlobalHotkey_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not TextBox tb) return;
        if (tb.Tag is not EGlobalHotkey eg) return;

        var item = _viewModel.GetKeyEventItem(eg);

        var key = e.Key;
        if (key is Windows.System.VirtualKey.Control or Windows.System.VirtualKey.Shift or Windows.System.VirtualKey.Menu)
        {
            key = Windows.System.VirtualKey.None;
        }

        item.KeyCode = key == Windows.System.VirtualKey.None ? null : (int)key;
        item.Control = IsKeyDown(Windows.System.VirtualKey.Control);
        item.Shift = IsKeyDown(Windows.System.VirtualKey.Shift);
        item.Alt = IsKeyDown(Windows.System.VirtualKey.Menu);

        tb.Text = KeyEventItemToString(item);
    }

    private static bool IsKeyDown(Windows.System.VirtualKey key)
    {
        return (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
    }

    private void BindData()
    {
        foreach (var tb in _textBoxes)
        {
            if (tb.Tag is not EGlobalHotkey eg) continue;
            var item = _viewModel.GetKeyEventItem(eg);
            tb.Text = KeyEventItemToString(item);
        }
    }

    private static string KeyEventItemToString(KeyEventItem? item)
    {
        if (item == null) return string.Empty;

        var parts = new List<string>();
        if (item.Control) parts.Add("Ctrl");
        if (item.Shift) parts.Add("Shift");
        if (item.Alt) parts.Add("Alt");
        if (item.KeyCode != null && item.KeyCode != (int)Windows.System.VirtualKey.None)
        {
            parts.Add(((Windows.System.VirtualKey)item.KeyCode).ToString());
        }
        return string.Join(" + ", parts);
    }
}
