using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Samba.Infrastructure.Settings;
using Samba.Localization.Properties;
using Samba.Services;

namespace Samba.Presentation.Common.ErrorReport
{
    class ErrorReportViewModel : ObservableObject
    {
        private bool? _dialogResult;
        public bool? DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        public ExceptionReportInfo Model { get; set; }

        public ErrorReportViewModel(IEnumerable<Exception> exceptions)
        {
            Model = new ExceptionReportInfo { AppAssembly = Assembly.GetCallingAssembly() };
            Model.SetExceptions(exceptions);

            string fileName = LocalSettings.TerminalName + string.Format("-ExceptionReport-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt",
                              DateTime.Now);
            String exceptionFile = Path.Combine(LocalSettings.UserPath, fileName);
            EMailService.SendEmail(ErrorReportAsText);

            SaveReportToFile(exceptionFile);
            //RuleExecutor.NotifyEvent(RuleEventNames.OnExceptionOccured, new { ParameterValues = ErrorReportAsText });
           

            CopyCommand = new CaptionCommand<string>(Resources.Copy, OnCopyCommand);
            SaveCommand = new CaptionCommand<string>(Resources.Save, OnSaveCommand);
            SubmitCommand = new CaptionCommand<string>(Resources.Send, OnSubmitCommand);
            RestartCommand = new CaptionCommand<string>(Resources.RestartApp, OnRestartCommand);
        }

        private void OnSubmitCommand(string obj)
        {
            if (string.IsNullOrEmpty(UserMessage))
            {
                if (MessageBox.Show(Resources.ErrorReportWithoutFeedback, Resources.Information, MessageBoxButton.YesNo) == MessageBoxResult.No) return;
            }
            DialogResult = false;
            SubmitError();
            
        }

        private void OnRestartCommand(string obj)
        {
            try
            {
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown(1);
            }
            catch (Exception)
            {
            }
            Environment.Exit(1);

        }

        

        public void SubmitError()
        {
            var tempFile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            //var tempFile = Path.GetTempFileName().Replace(".tmp", ".txt");
            SaveReportToFile(tempFile);
            string queryString = string.Format("from={0}&emaila={1}&file={2}",
                Uri.EscapeDataString("info@sambapos.com"),
                Uri.EscapeDataString("SambaPOS Error Report"),
                Uri.EscapeDataString(tempFile));

            var c = new WebClient();
            var result = c.UploadFile("http://reports.sambapos.com/file.php?" + queryString, "POST", tempFile);
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch(Exception ex)
                {
                    AppServices.SaveExceptionToFile(ex,
                        "Failed to delete Temporary file:" + tempFile + " generated to submit Error report");
                   
                }
            }
            MessageBox.Show(Encoding.ASCII.GetString(result));
        }

        private void OnSaveCommand(string obj)
        {
            var sf = new SaveFileDialog() { DefaultExt = ".txt" };
            if (sf.ShowDialog() == true)
            {
                SaveReportToFile(sf.FileName);
            }
        }

        private void OnCopyCommand(object obj)
        {
            Clipboard.SetText(ErrorReportAsText);
        }

        public ICaptionCommand CopyCommand { get; set; }
        public ICaptionCommand SaveCommand { get; set; }
        public ICaptionCommand SubmitCommand { get; set; }
        public ICaptionCommand CloseCommand { get; set; }
        public ICaptionCommand RestartCommand { get; set; }

        private string _errorReportAsText;
        public string ErrorReportAsText
        {
            get { return _errorReportAsText ?? (_errorReportAsText = GenerateReport()); }
            set { _errorReportAsText = value; }
        }

        public string ErrorMessage { get { return Model.MainException.Message; } }

        public string UserMessage
        {
            get { return Model.UserExplanation; }
            set
            {
                Model.UserExplanation = value;
                _errorReportAsText = null;
                RaisePropertyChanged("ErrorReportAsText");
            }
        }

        private string GenerateReport()
        {
            var rg = new ExceptionReportGenerator(Model);
            return rg.CreateExceptionReport();
        }

        public string GetErrorReport()
        {
            _errorReportAsText = null;
            return ErrorReportAsText;
        }

        public void SaveReportToFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            try
            {
                using (var stream = File.OpenWrite(fileName))
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(ErrorReportAsText);
                    writer.Flush();
                }
            }
            catch (Exception exception)
            {
                //MessageBox.Show(string.Format("Unable to save file '{0}' : {1}", fileName, exception.Message));
            }
        }


    }
}
