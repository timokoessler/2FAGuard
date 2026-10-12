using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Guard.Avalonia.ViewModels;

namespace Guard.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}