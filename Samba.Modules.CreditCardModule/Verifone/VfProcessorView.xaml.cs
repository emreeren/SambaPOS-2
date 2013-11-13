using System.Windows;
using Samba.Presentation.Common;

namespace Samba.Modules.CreditCardModule.Verifone
{
    /// <summary>
    /// Interaction logic for ExternalProcessorView.xaml
    /// </summary>

    public partial class VfProcessorView : Window
    {
        public VfProcessorView(VfProcessorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void VfProcessorWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
        }
        
    }
}
