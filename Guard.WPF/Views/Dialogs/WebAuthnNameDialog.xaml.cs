using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Dialogs;

public partial class WebAuthnNameDialog : ContentDialog
{
    public WebAuthnNameDialog(ContentDialogHost contentDialogHost)
        : base(contentDialogHost)
    {
        InitializeComponent();
        this.NameBox.Focus();
    }

    internal string GetName()
    {
        return NameBox.Text;
    }
}
