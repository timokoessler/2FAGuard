using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using RoutedEventArgs = Avalonia.Interactivity.RoutedEventArgs;

namespace Guard.Avalonia.Views.Controls;

public partial class TokenCard : UserControl
{
    public TokenCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TokenViewModel vm)
        {
            vm.CopyCode();
        }
    }
}

public class TokenViewModel
{
    public string Name { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string CurrentCode { get; set; } = "000000";
    public double TimeRemaining { get; set; } = 30;
    public Bitmap? Icon { get; set; }
    
    public void CopyCode()
    {
        // TODO: Copy to clipboard
    }
}