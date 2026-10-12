using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Guard.Avalonia.ViewModels;
using Guard.Avalonia.Views.Pages.Add;
using RoutedEventArgs = Avalonia.Interactivity.RoutedEventArgs;

namespace Guard.Avalonia.Views.Pages;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnAddTokenClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new AddOverviewPage());
        }
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new SettingsPage());
        }
    }
}