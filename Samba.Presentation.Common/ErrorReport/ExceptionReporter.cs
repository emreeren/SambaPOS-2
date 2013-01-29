using System;
using System.Windows;

namespace Samba.Presentation.Common.ErrorReport
{
    public static class ExceptionReporter
    {
        public static void Show(params Exception[] exceptions)
        {
            if (exceptions == null) return;
            try
            {
                var viewModel = new ErrorReportViewModel(exceptions);
                var view = new ErrorReportView { DataContext = viewModel };
                view.ShowDialog();
            }
            catch (Exception internalException)
            {
                MessageBox.Show(internalException.Message);
            }
        }
    }
}
