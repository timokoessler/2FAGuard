using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Guard.Avalonia.ViewModels;

namespace Guard.Avalonia.Views.Pages.Add;

public partial class TokenSuccessPage : UserControl
{
    public TokenSuccessPage()
    {
        InitializeComponent();
    }

    public TokenSuccessPage(int tokenID, int total = 0, int duplicate = 0) : this()
    {
        if (total == 0 && tokenID > 0)
        {
            // Single token added
            TitleText.Text = "Token Added";
            MessageText.Text = "Your token has been successfully added.";
        }
        else if (total > 0)
        {
            // Multiple tokens added
            TitleText.Text = "Import Complete";
            if (duplicate > 0)
            {
                MessageText.Text = $"{total} tokens imported ({duplicate} duplicates skipped).";
            }
            else
            {
                MessageText.Text = $"{total} tokens imported successfully.";
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDoneClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new HomePage());
        }
    }
}