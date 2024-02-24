using System.Windows.Controls;
using TOTPTokenGuard.Core.Icons;

namespace TOTPTokenGuard.Views.Pages.Add
{
    /// <summary>
    /// Interaktionslogik für AddManual.xaml
    /// </summary>
    public partial class AddManual : Page
    {
        public AddManual()
        {
            InitializeComponent();
            IconSvgView.SvgSource = IconManager.GetIcon("default", IconManager.IconColor.Colored);
        }
    }
}
