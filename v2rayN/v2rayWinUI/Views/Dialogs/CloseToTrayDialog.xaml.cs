using Microsoft.UI.Xaml.Controls;

namespace v2rayWinUI.Views.Dialogs;

public sealed partial class CloseToTrayDialog : ContentDialog
{
    public CloseToTrayDialog()
    {
        InitializeComponent();
    }

    public bool RememberChoice => ChkRemember.IsChecked == true;
}
