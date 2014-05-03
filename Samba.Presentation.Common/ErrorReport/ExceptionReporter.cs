using System;
using System.IO;
using System.Linq;
using System.Windows;
using Samba.Infrastructure.Settings;
using Samba.Presentation.Common.Services;
using Samba.Services;

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
                
                try
                {
                    
                    AppServices.LogError(exceptions.FirstOrDefault(), "Error while processsing.");
                    return;
                }
                catch (Exception)
                {
                }
                var view = new ErrorReportView { DataContext = viewModel };
                view.ShowDialog();
            }
            catch (Exception internalException)
            {
                InteractionService.UserIntraction.GiveFeedback(internalException.Message);
            }
        }
    }
}
