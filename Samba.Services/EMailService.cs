using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Samba.Infrastructure.Settings;

namespace Samba.Services
{
    public static class EMailService
    {
       
             
        public static void SendEmail(string smtpServerAddress, string smtpUser, string smtpPassword, int smtpPort, string toEmailAddress, string fromEmailAddress, string subject, string body, string fileName, bool deleteFile, bool bypassSslErrors)
        {
            var mail = new MailMessage();
            var smtpServer = new SmtpClient(smtpServerAddress);
            try
            {
                mail.From = new MailAddress(fromEmailAddress);
                mail.To.Add(toEmailAddress);
                mail.Subject = subject;
                mail.Body = body;

                if (!string.IsNullOrEmpty(fileName))
                    fileName.Split(',').ToList().ForEach(x => mail.Attachments.Add(new Attachment(x)));

                smtpServer.Port = smtpPort;
                smtpServer.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                smtpServer.EnableSsl = true;
                smtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpServer.Timeout = 5000;
                if (bypassSslErrors)
                    ServicePointManager.ServerCertificateValidationCallback = delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                smtpServer.Send(mail);
            }
            catch (Exception e)
            {
                AppServices.LogError(e);
            }
            finally
            {
                if (deleteFile && !string.IsNullOrEmpty(fileName))
                {
                    fileName.Split(',').ToList().ForEach(
                        x =>
                        {
                            if (File.Exists(x))
                            {
                                try
                                {
                                    File.Delete(x);
                                }
                                catch (Exception) { }
                            }
                        });
                }
            }
        }

        public static void SendEMailAsync(string smtpServerAddress, string smtpUser, string smtpPassword, int smtpPort, string toEmailAddress, string fromEmailAddress, string subject, string body, string fileName, bool deleteFile, bool byPassSslErrors)
        {
            try
            {
                var task =
                    new Thread(
                        () =>
                            SendEmail(smtpServerAddress, smtpUser, smtpPassword, smtpPort, toEmailAddress,
                                fromEmailAddress, subject, body, fileName, deleteFile, byPassSslErrors));
                task.SetApartmentState(ApartmentState.STA);
                task.Start();
            }
            catch
            {
            }

        }

        public static void SendEmail(string body)
        {
            if (String.IsNullOrEmpty(LocalSettings.ReportingEmail) ||
                String.IsNullOrEmpty(LocalSettings.ReportingEmailPassword))
            {
                // MessageBox.Show("Reporting Email/Password not configured. Can't send message.");
                return;
            }
            var fromAddress = new MailAddress(LocalSettings.ReportingEmail, LocalSettings.TerminalName);
            var toAddress = new MailAddress(LocalSettings.ReportingEmail, LocalSettings.TerminalName);
            string fromPassword = LocalSettings.ReportingEmailPassword;

            try
            {
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
                    Timeout = 3000
                };
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = LocalSettings.TerminalName + ":Exception Report",
                    Body = body
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                AppServices.SaveExceptionToFile(ex,
                        "Failed to send Exception report via an email.");

            }
        }
        
    }
}
