using System.Windows;
using Samba.Presentation.Common;

namespace Samba.Modules.CreditCardModule.ExternalProcessor
{
    /// <summary>
    /// Interaction logic for ExternalProcessorView.xaml
    /// </summary>

    public partial class ExternalProcessorView : Window
    {
        public ExternalProcessorView(ExternalProcessorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void ExternalProcessorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SwipeDataBox.BackgroundFocus();
        }
    }
}
