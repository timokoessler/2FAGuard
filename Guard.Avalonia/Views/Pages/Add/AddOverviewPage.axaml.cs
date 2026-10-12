using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Guard.Avalonia.ViewModels;

namespace Guard.Avalonia.Views.Pages.Add;

public partial class AddOverviewPage : UserControl
{
    public AddOverviewPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new HomePage());
        }
    }

    private void OnScanQrClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement QR code scanning
    }

    private void OnManualClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Navigate to manual entry page
    }

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new Views.Pages.Start.ImportPage());
        }
    }

    private void OnSteamClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement Steam Guard
    }
}