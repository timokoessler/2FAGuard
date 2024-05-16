// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Text;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Controls;

public partial class PasswordDialog : ContentDialog
{
    public PasswordDialog(ContentPresenter contentPresenter)
        : base(contentPresenter)
    {
        InitializeComponent();
        this.PasswordBox.Focus();
    }

    internal byte[] GetPassword()
    {
        return Encoding.UTF8.GetBytes(PasswordBox.Password);
    }
}