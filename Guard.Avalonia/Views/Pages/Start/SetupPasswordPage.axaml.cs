using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Guard.Avalonia.ViewModels;
using RoutedEventArgs = Avalonia.Interactivity.RoutedEventArgs;

namespace Guard.Avalonia.Views.Pages.Start;

public partial class SetupPasswordPage : UserControl
{
    public SetupPasswordPage()
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
            vm.NavigateTo(new WelcomePage());
        }
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        var confirmBox = this.FindControl<TextBox>("ConfirmPasswordBox");
        var errorText = this.FindControl<TextBlock>("ErrorText");
        
        var password = passwordBox?.Text ?? "";
        var confirm = confirmBox?.Text ?? "";
        
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Please enter a password");
            return;
        }
        
        if (password != confirm)
        {
            ShowError("Passwords do not match");
            return;
        }
        
        if (password.Length < 8)
        {
            ShowError("Password must be at least 8 characters");
            return;
        }
        
        // TODO: Initialize vault with password
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new HomePage());
        }
    }
    
    private void ShowError(string message)
    {
        var errorText = this.FindControl<TextBlock>("ErrorText");
        if (errorText != null)
        {
            errorText.Text = message;
            errorText.IsVisible = true;
        }
    }
}