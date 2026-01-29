using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Enums;
using ServiceLib.ViewModels;

namespace v2rayWinUI.Views;

public sealed partial class MsgView : Page
{
    private readonly MsgViewModel _viewModel;

    public MsgView()
    {
        InitializeComponent();

        _viewModel = new MsgViewModel(UpdateViewHandler);

        btnCopyAll.Click += (s, e) =>
        {
            var data = txtMsg.Text;
            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.SetText(data);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
        };

        btnClear.Click += (s, e) =>
        {
            txtMsg.Text = "----- Message cleared -----\n";
        };

        txtMsgFilter.TextChanged += (s, e) => _viewModel.MsgFilter = txtMsgFilter.Text;
        togAutoRefresh.Toggled += (s, e) => _viewModel.AutoRefresh = togAutoRefresh.IsOn;
    }

    private Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        switch (action)
        {
            case EViewAction.DispatcherShowMsg:
                if (obj is not string msg) break;
                DispatcherQueue.TryEnqueue(() =>
                {
                    AppendMsg(msg);
                });
                break;
        }
        return Task.FromResult(true);
    }

    private void AppendMsg(string msg)
    {
        if (GetApproxLineCount(txtMsg.Text) > _viewModel.NumMaxMsg)
        {
            txtMsg.Text = "";
        }

        txtMsg.Text += msg;
        if (togScrollToEnd.IsOn)
        {
            txtMsg.SelectionStart = txtMsg.Text.Length;
            // WinUI3 TextBox does not have ScrollToEnd; setting SelectionStart generally keeps view at caret.
            txtMsg.SelectionLength = 0;
        }
    }

    private static int GetApproxLineCount(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        // WinUI TextBox doesn't expose LineCount reliably; approximate by counting line breaks.
        var count = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n') count++;
        }
        return count;
    }
}
