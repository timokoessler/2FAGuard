using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Guard.Avalonia.Views.Pages.Start;
using Guard.Avalonia.Views.Pages;

namespace Guard.Avalonia.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private Control _currentPage = new WelcomePage();

    public Control CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel()
    {
        // Start with welcome page for first-time setup
        CurrentPage = new WelcomePage();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void NavigateTo(Control page)
    {
        CurrentPage = page;
    }
}