﻿using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Views.UIComponents;

namespace Guard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für TokenSuccessPage.xaml
    /// </summary>
    public partial class TokenSuccessPage : Page
    {
        private readonly MainWindow mainWindow;

        public TokenSuccessPage()
        {
            InitializeComponent();
            int tokenID = (int)NavigationContextManager.CurrentContext["tokenID"];
            string type = (string)NavigationContextManager.CurrentContext["type"];

            mainWindow = (MainWindow)Application.Current.MainWindow;

            if (type.Equals("added"))
            {
                mainWindow.SetPageTitle(I18n.GetString("stp.added"));
            }
            else if (type.Equals("edited"))
            {
                mainWindow.SetPageTitle(I18n.GetString("stp.edited"));
            }
            else if (type.Equals("added-multiple"))
            {
                mainWindow.SetPageTitle(I18n.GetString("stp.added.multiple"));
            }

            if (!type.Equals("added-multiple"))
            {
                TOTPTokenHelper? token = TokenManager.GetTokenById(tokenID);
                if (token == null)
                {
                    mainWindow.Navigate(typeof(Home));
                    return;
                }

                TokenCardContainer.Children.Insert(1, new TokenCard(token));
            }
            else
            {
                int count = (int)NavigationContextManager.CurrentContext["count"];
                int duplicateCount = (int)NavigationContextManager.CurrentContext["duplicateCount"];

                int addedCount = count - duplicateCount;

                Wpf.Ui.Controls.TextBlock textBlock =
                    new()
                    {
                        Text = I18n.GetString("i.stp.added.multiple.description")
                            .Replace("@Count", addedCount.ToString()),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 16
                    };
                TokenCardContainer.Children.Insert(1, textBlock);

                if (duplicateCount > 0)
                {
                    Wpf.Ui.Controls.TextBlock duplicateTextBlock =
                        new()
                        {
                            Text = I18n.GetString("i.stp.added.multiple.duplicate")
                                .Replace("@Count", duplicateCount.ToString()),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 16
                        };
                    TokenCardContainer.Children.Insert(2, duplicateTextBlock);
                }
            }

            NavigationContextManager.ClearContext();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(typeof(Home));
        }
    }
}