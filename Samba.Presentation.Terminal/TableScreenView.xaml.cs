using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Samba.Presentation.Common;

namespace Samba.Presentation.Terminal
{
    /// <summary>
    /// Interaction logic for TableView.xaml
    /// </summary>
    public partial class TableScreenView : UserControl
    {
        public TableScreenView()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ts = (DataContext as TableScreenViewModel);
            if (ts != null)
            {
                if (ts.SelectedTableScreen.NumeratorHeight < 30)
                    ts.DisplayFullScreenNumerator();
            }
        }

        private void TableScreenView_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var ts = (DataContext as TableScreenViewModel);
            if (ts != null)
            {
                e.Handled = ts.HandleTextInput(e.Text);
            }
        }

        private void TableScreenView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var ts = (DataContext as TableScreenViewModel);
            if (ts != null && ts.SelectedTableScreen != null)
            {
                if (ts.SelectedTableScreen.NumeratorHeight > 0)
                {
                    ts.NumeratorValue = "";
                    NumeratorTextEdit.BackgroundFocus();
                }
            }

        }
    }
}
