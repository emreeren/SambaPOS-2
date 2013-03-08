using System.Windows;
using System.Windows.Controls;
using Samba.Domain.Models.Users;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;

namespace Samba.Login
{
    public delegate void PinSubmittedEventHandler(object sender, PinData pinData);

    /// <summary>
    /// Interaction logic for LoginPadControl.xaml
    /// </summary>
    public partial class LoginPadControl : UserControl
    {
        public event PinSubmittedEventHandler PinSubmitted;
        private string _pinValue = string.Empty;

        public LoginPadControl()
        {
            InitializeComponent();
            PinValue = EmptyString;
        }

        private string PinValue { get { return _pinValue; } set { _pinValue = value; UpdatePinTextBox(_pinValue); } }
        private static string EmptyString { get { return " " + Localization.Properties.Resources.EnterPin; } }

        private void UpdatePinTextBox(string pinValue)
        {
            PinTextBox.Text = pinValue == EmptyString ? pinValue : "".PadLeft(pinValue.Length, '*');
        }

        private bool CheckPinValue()
        {
            if (_pinValue == EmptyString)
                PinValue = "";
            return _pinValue.Length < 9;
        }

        public void UpdatePinValue(string value)
        {
            if (CheckPinValue())
            {
                PinValue += value;
            }
        }

        public void SubmitPin(int timeCardAction)
        {
            if (PinSubmitted != null && AppServices.CanStartApplication())
                PinSubmitted(this, new PinData { PinCode = _pinValue, TimeCardAction = timeCardAction });
            else
            {
                if (!AppServices.CanStartApplication())
                    MessageBox.Show(Localization.Properties.Resources.CheckDBVersion);
            }
            PinValue = EmptyString;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SubmitPin(1); //Clock In
        }

        private void Button_ClockOut(object sender, RoutedEventArgs e)
        {
            bool answer = InteractionService.UserIntraction.AskQuestion(
                       Localization.Properties.Resources.ConfirmClockOut);
            if (answer)
            {
                SubmitPin(2); //Clock Out
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("1");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("2");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("3");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("4");
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("5");
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("6");
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("7");
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("8");
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("9");
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            UpdatePinValue("0");
        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            PinValue = "";
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PinTextBox.BackgroundFocus();
        }
    }
}
