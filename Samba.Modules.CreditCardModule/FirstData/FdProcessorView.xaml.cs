using System.Windows;
using Samba.Modules.CreditCardModule.ExternalProcessor;
using Samba.Presentation.Common;

namespace Samba.Modules.CreditCardModule.FirstData
{
    /// <summary>
    /// Interaction logic for ExternalProcessorView.xaml
    /// </summary>

    public partial class FdProcessorView : Window
    {
        public FdProcessorView(FdProcessorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void FdProcessorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CardNumber.BackgroundFocus();
        }
    }
}
