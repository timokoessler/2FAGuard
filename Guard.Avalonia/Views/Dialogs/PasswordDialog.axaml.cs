using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace Guard.Avalonia.Views.Dialogs
{
    public partial class PasswordDialog : Window
    {
        public string? Password { get; private set; }
        public bool Result { get; private set; }

        public PasswordDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public PasswordDialog(string description) : this()
        {
            DescriptionText.Text = description;
        }

        public void SetDescription(string description)
        {
            DescriptionText.Text = description;
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Text;
            Result = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        public byte[]? GetPassword()
        {
            if (string.IsNullOrEmpty(Password))
                return null;
            return System.Text.Encoding.UTF8.GetBytes(Password);
        }
    }
}