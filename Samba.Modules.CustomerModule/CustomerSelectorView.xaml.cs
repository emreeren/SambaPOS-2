using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Samba.Presentation.Common;

namespace Samba.Modules.CustomerModule
{
    /// <summary>
    /// Interaction logic for CustomerSelectorView.xaml
    /// </summary>

    [Export]
    public partial class CustomerSelectorView : UserControl
    {
        readonly DependencyPropertyDescriptor _selectedIndexChange = DependencyPropertyDescriptor.FromProperty(Selector.SelectedIndexProperty, typeof(TabControl));

        [ImportingConstructor]
        public CustomerSelectorView(CustomerSelectorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _selectedIndexChange.AddValueChanged(MainTabControl, MyTabControlSelectedIndexChanged);
        }

        private void MyTabControlSelectedIndexChanged(object sender, EventArgs e)
        {
            if (((TabControl)sender).SelectedIndex == 1)
                PhoneNumberTextBox.BackgroundFocus();
        }

        private void FlexButton_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void Reset()
        {
            ((CustomerSelectorViewModel)DataContext).RefreshSelectedCustomer();
            PhoneNumber.BackgroundFocus();
        }

        private void HandleKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (((CustomerSelectorViewModel)DataContext).SelectCustomerCommand.CanExecute(""))
                    ((CustomerSelectorViewModel)DataContext).SelectCustomerCommand.Execute("");
            }
        }

        private void PhoneNumber_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDown(e);
        }

        private void CustomerName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDown(e);
        }

        private void Address_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDown(e);
        }

        private void PhoneNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDown(e);
        }

        private void TicketNo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                e.Handled = true;
                if (((CustomerSelectorViewModel)DataContext).FindTicketCommand.CanExecute(""))
                    ((CustomerSelectorViewModel)DataContext).FindTicketCommand.Execute("");
            }
        }

        private void PhoneNumber_Loaded(object sender, RoutedEventArgs e)
        {
            PhoneNumber.BackgroundFocus();
        }
    }
}
