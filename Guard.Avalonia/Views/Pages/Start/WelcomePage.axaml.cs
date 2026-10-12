using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Guard.Avalonia.ViewModels;
using RoutedEventArgs = Avalonia.Interactivity.RoutedEventArgs;

namespace Guard.Avalonia.Views.Pages.Start;

public partial class WelcomePage : UserControl
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCreateNewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new SetupPasswordPage());
        }
    }

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new ImportPage());
        }
    }
}