﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Samba.Domain.Models.Users;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;

namespace Samba.Login
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>

    [Export]
    public partial class LoginView : UserControl
    {
        private readonly LoginViewModel _viewModel;

        [ImportingConstructor]
        public LoginView(LoginViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = viewModel;
        }

        private void LoginPadControl_PinSubmitted(object sender, PinData pinData)
        {
            _viewModel.SubmitPin(pinData);
        }

        private void UserControl_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text) && char.IsDigit(e.Text, 0))
                PadControl.UpdatePinValue(e.Text);
        }

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PadControl.SubmitPin(1);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Uri u = new Uri(Localization.Properties.Resources.ClientServerConnectionHelpUrlString);
            Process.Start(new ProcessStartInfo(u.AbsoluteUri));
            e.Handled = true;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ButtonBase2_OnClick(object sender, RoutedEventArgs e)
        {
            bool answer = InteractionService.UserIntraction.AskQuestion(
                Localization.Properties.Resources.ConfirmClockOut);
            if (answer)
            {
                PadControl.SubmitPin(2); //Clock Out
            }
        }
    }
}
