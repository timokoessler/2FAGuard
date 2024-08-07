using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Dialogs;

public partial class WebAuthnNameDialog : ContentDialog
{
    public WebAuthnNameDialog(ContentPresenter contentPresenter)
        : base(contentPresenter)
    {
        InitializeComponent();
        this.NameBox.Focus();
    }

    internal string GetName()
    {
        return NameBox.Text;
    }
}
