﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Users;
using Samba.Infrastructure.Data;
using Samba.Infrastructure.Settings;
using Samba.Persistance.Data;

namespace Samba.Services
{
    public enum AppScreens
    {
        LoginScreen,
        Navigation,
        SingleTicket,
        TicketList,
        Payment,
        TableList,
        CustomerList,
        WorkPeriods,
        Dashboard,
        CashView
    }

    public static class AppServices
    {
        public static Dispatcher MainDispatcher { get; set; }
        public static AppScreens ActiveAppScreen { get; set; }

        private static IWorkspace _workspace;
        public static IWorkspace Workspace
        {
            get { return _workspace ?? (_workspace = WorkspaceFactory.Create()); }
            set { _workspace = value; }
        }

        private static MainDataContext _mainDataContext;
        public static MainDataContext MainDataContext
        {
            get { return _mainDataContext ?? (_mainDataContext = new MainDataContext()); }
            set { _mainDataContext = value; }
        }

        private static PrinterService _printService;
        public static PrinterService PrintService
        {
            get { return _printService ?? (_printService = new PrinterService()); }
        }

        private static DataAccessService _dataAccessService;
        public static DataAccessService DataAccessService
        {
            get { return _dataAccessService ?? (_dataAccessService = new DataAccessService()); }
        }

        private static MessagingService _messagingService;
        public static MessagingService MessagingService
        {
            get { return _messagingService ?? (_messagingService = new MessagingService()); }
        }

        private static CashService _cashService;
        public static CashService CashService
        {
            get { return _cashService ?? (_cashService = new CashService()); }
        }

        private static SettingService _settingService;
        public static SettingService SettingService
        {
            get { return _settingService ?? (_settingService = new SettingService()); }
        }

        private static IEnumerable<Terminal> _terminals;
        public static IEnumerable<Terminal> Terminals { get { return _terminals ?? (_terminals = Workspace.All<Terminal>()); } }

        private static Terminal _terminal;
        public static Terminal CurrentTerminal { get { return _terminal ?? (_terminal = GetCurrentTerminal()); } set { _terminal = value; } }

        private static User _currentLoggedInUser;
        public static User CurrentLoggedInUser
        {
            get { return _currentLoggedInUser ?? User.Nobody; }
            private set { _currentLoggedInUser = value; }
        }

        private static bool _processRestarting;
        private static bool _displayingNetworkError;

        public static bool CanNavigate()
        {
            return MainDataContext.SelectedTicket == null;
        }

        public static bool CanStartApplication()
        {
            return LocalSettings.CurrentDbVersion <= 0 || LocalSettings.CurrentDbVersion == LocalSettings.DbVersion;
        }

        public static bool CanModifyTicket()
        {
            return true;
        }

        private static User GetUserByPinCode(string pinCode)
        {
            return Workspace.All<User>(x => x.PinCode == pinCode).FirstOrDefault();
        }

        private static LoginStatus CheckPinCodeStatus(string pinCode)
        {
            var user = Workspace.Single<User>(x => x.PinCode == pinCode);
            return user == null ? LoginStatus.PinNotFound : LoginStatus.CanLogin;
        }

        private static Terminal GetCurrentTerminal()
        {
            if (!string.IsNullOrEmpty(LocalSettings.TerminalName))
            {
                var terminal = Terminals.SingleOrDefault(x => x.Name == LocalSettings.TerminalName);
                if (terminal != null) return terminal;
            }
            var dterminal = Terminals.SingleOrDefault(x => x.IsDefault);
            return dterminal ?? Terminal.DefaultTerminal;
        }

        public static void LogError(Exception e)
        {
            try
            {
                LogError(e, e.Message);
            }
            catch (Exception ex)
            {
            }

        }

        public static void LogError(Exception e, string userMessage)
        {
            try
            {
                if (CheckIfSQLNetworkException(e))
                {
                    if (!_displayingNetworkError)
                    {
                        _displayingNetworkError = true;
                        ConfirmRestartProcess(e);
                        _displayingNetworkError = false;
                    }
                    return;
                }
            }
            catch (Exception ex)
            {

            }
            if (e.InnerException != null)
            {
                MessageBox.Show(userMessage + ":" + e.InnerException.Message, "Error", MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(userMessage + ":" + e.Message, "Error", MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
            Logger.Write(e, "General");
        }
       

        private static bool CheckIfSQLNetworkException(Exception ex)
        {
            const string  networkError = "A network-related or instance-specific error occurred while establishing a connection to SQL Server";
            if (ex == null)
            {
                return false;
            }
            if (ex.InnerException != null)
            {
                if (ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message != null)
                {
                    if (ex.InnerException.InnerException.Message.Contains(networkError))
                    {
                        return true;
                    }
                }
                else
                {
                    if ((ex.InnerException.Message != null) && ex.InnerException.Message.Contains(networkError))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (ex.InnerException.Message.Contains(networkError))
                {
                    return true;
                }
            }
            return false;
        }

         private static void ConfirmRestartProcess(Exception e)
        {
            if (e == null) return;
            try
            {
                EMailService.SendEmail(e.StackTrace);
                
                if(MessageBox.Show("Failed to connect to SQL Server. Do you want to RESTART Application?", "Network Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    RestartProcess();
                }
            }
            catch (Exception)
            {
            }
        }

        private static void RestartProcess()
        {
            if (!_processRestarting)
            {
                _processRestarting = true;
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Environment.Exit(1);
            }
        }

        public static void Log(string message)
        {
            Logger.Write(message, "General", 0, 0, TraceEventType.Verbose);
        }

        public static void Log(string message, string category)
        {
            Logger.Write(message, category);
        }
        public static void LogExcetion(Exception ex, string userMessage)
        {
            Logger.Write(ex, "General");
        }

        public static void ResetCache()
        {
            _terminal = null;
            _terminals = null;
            MainDataContext.ResetCache();
            PrintService.ResetCache();
            SettingService.ResetCache();
            SerialPortService.ResetCache();
            Dao.ResetCache();
            Workspace = WorkspaceFactory.Create();
        }

        public static User LoginUser(string pinValue)
        {
            Debug.Assert(CurrentLoggedInUser == User.Nobody);
            CurrentLoggedInUser = CanStartApplication() && CheckPinCodeStatus(pinValue) == LoginStatus.CanLogin ? GetUserByPinCode(pinValue) : User.Nobody;
            MainDataContext.ResetUserData();
            return CurrentLoggedInUser;
        }

        public static void LogoutUser(bool resetCache = true)
        {
            Debug.Assert(CurrentLoggedInUser != User.Nobody);
            CurrentLoggedInUser = User.Nobody;
            if (resetCache) ResetCache();
        }

        public static bool IsUserPermittedFor(string p)
        {
            if (CurrentLoggedInUser.UserRole.IsAdmin) return true;
            if (CurrentLoggedInUser.UserRole.Id == 0) return false;
            var permission = CurrentLoggedInUser.UserRole.Permissions.SingleOrDefault(x => x.Name == p);
            if (permission == null) return false;
            return permission.Value == (int)PermissionValue.Enabled;
        }

        public static void SaveExceptionToFile(Exception ex, string userMessage)
        {
            string fileName = string.Format(LocalSettings.TerminalName + "-ExceptionReport-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt", DateTime.Now);

            try
            {
                using (var stream = File.OpenWrite(fileName))
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(userMessage + ":" + ex.Message + Environment.NewLine);
                    writer.Write(ex.StackTrace);
                    writer.Flush();
                }
            }
            catch 
            {
               
            }
        }
    }
}
