using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows.Input;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;
using System.Linq;

namespace Samba.Modules.SettingsModule.WorkPeriods
{
    [Export]
    public class WorkPeriodsViewModel : ObservableObject
    {
        private IEnumerable<WorkPeriodViewModel> _workPeriods;
        public IEnumerable<WorkPeriodViewModel> WorkPeriods
        {
            get
            {
                return _workPeriods ?? (_workPeriods = Dao.Last<WorkPeriod>(30)
                                                           .OrderByDescending(x => x.Id)
                                                           .Select(x => new WorkPeriodViewModel(x)));
            }

        }

        public ICaptionCommand StartOfDayCommand { get; set; }
        public ICaptionCommand EndOfDayCommand { get; set; }
        public ICaptionCommand DisplayStartOfDayScreenCommand { get; set; }
        public ICaptionCommand DisplayEndOfDayScreenCommand { get; set; }
        public ICaptionCommand CancelCommand { get; set; }
        public ICaptionCommand RequestShutdownCommand { get; set; }

        public WorkPeriod LastWorkPeriod { get { return AppServices.MainDataContext.CurrentWorkPeriod; } }

        public TimeSpan WorkPeriodTime { get; set; }

        private int _openTicketCount;
        public int OpenTicketCount
        {
            get { return _openTicketCount; }
            set { _openTicketCount = value; RaisePropertyChanged("OpenTicketCount"); }
        }

        private string _openTicketLabel;
        public string OpenTicketLabel
        {
            get { return _openTicketLabel; }
            set { _openTicketLabel = value; RaisePropertyChanged("OpenTicketLabel"); }
        }

        private int _activeScreen;
        public int ActiveScreen
        {
            get { return _activeScreen; }
            set { _activeScreen = value; RaisePropertyChanged("ActiveScreen"); }
        }

        public string ConnectionCountLabel
        {
            get
            {
                return string.Format(Resources.ActiveConnections_f, AppServices.MessagingService.ConnectionCount);
            }
        }

        public string StartDescription { get; set; }
        public string EndDescription { get; set; }
        public decimal CashAmount { get; set; }
        public decimal CreditCardAmount { get; set; }
        public decimal TicketAmount { get; set; }
        public bool ShutdownRequested { get; set; }
        public bool IsShutdownRequestVisible
        {
            get
            {
                if (AppServices.MainDataContext.IsCurrentWorkPeriodOpen)
                    return AppServices.MessagingService.ConnectionCount > 1;
                return false;
            }
        }

        public string LastEndOfDayLabel
        {
            get
            {
                if (AppServices.MainDataContext.IsCurrentWorkPeriodOpen)
                {
                    var title1 = string.Format(Resources.WorkPeriodStartDate_f, LastWorkPeriod.StartDate.ToShortDateString());
                    var title2 = string.Format(Resources.WorkPeriodStartTime, LastWorkPeriod.StartDate.ToShortTimeString());
                    var title3 = string.Format(Resources.TotalWorkTimeDays_f, WorkPeriodTime.Days, WorkPeriodTime.Hours, WorkPeriodTime.Minutes);
                    if (WorkPeriodTime.Days == 0) title3 = string.Format(Resources.TotalWorkTimeHours_f, WorkPeriodTime.Hours, WorkPeriodTime.Minutes);
                    if (WorkPeriodTime.Hours == 0) title3 = string.Format(Resources.TotalWorkTimeMinutes_f, WorkPeriodTime.TotalMinutes);
                    return title1 + "\r\n" + title2 + "\r\n" + title3;
                }
                return Resources.StartWorkPeriodToEnablePos;
            }
        }

        public WorkPeriodsViewModel()
        {
            StartOfDayCommand = new CaptionCommand<string>(Resources.StartWorkPeriod, OnStartOfDayExecute, CanStartOfDayExecute);
            EndOfDayCommand = new CaptionCommand<string>(Resources.EndWorkPeriod, OnEndOfDayExecute, CanEndOfDayExecute);
            DisplayStartOfDayScreenCommand = new CaptionCommand<string>(Resources.StartWorkPeriod, OnDisplayStartOfDayScreenCommand, CanStartOfDayExecute);
            DisplayEndOfDayScreenCommand = new CaptionCommand<string>(Resources.EndWorkPeriod, OnDisplayEndOfDayScreenCommand, CanEndOfDayExecute);
            CancelCommand = new CaptionCommand<string>(Resources.Cancel, OnCancel);
            RequestShutdownCommand = new CaptionCommand<string>(Resources.RequestShutdown, OnShutDownRequested, CanRequestShutDown);
        }

        private bool CanRequestShutDown(string arg)
        {
            return _timer == null;
        }

        private Timer _timer;
        private void OnShutDownRequested(string obj)
        {
            AppServices.MessagingService.SendMessage(Messages.ShutdownRequest, "0");
            _timer = new Timer(OnTimer, null, 500, 5000);
        }

        private void OnTimer(object state)
        {
            AppServices.MessagingService.SendMessage(Messages.PingMessage, "1");
            RaisePropertyChanged("ConnectionCountLabel");
            if (AppServices.MessagingService.ConnectionCount < 2)
                StopTimer();
            AppServices.MainDispatcher.Invoke(new Action(Refresh));
        }

        private void StopTimer()
        {
            RaisePropertyChanged("IsShutdownRequestVisible");
            if (_timer != null)
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer = null;
        }

        private void OnCancel(string obj)
        {
            StopTimer();
            ActiveScreen = 0;
        }

        private void OnDisplayEndOfDayScreenCommand(string obj)
        {
            ActiveScreen = 2;
        }

        private void OnDisplayStartOfDayScreenCommand(string obj)
        {
            ActiveScreen = 1;
        }

        private bool CanEndOfDayExecute(string arg)
        {
            return AppServices.ActiveAppScreen == AppScreens.WorkPeriods
                && OpenTicketCount == 0
                && (AppServices.MessagingService.ConnectionCount < 2 || AppServices.IsUserPermittedFor(PermissionNames.CloseActiveWorkPeriods))
                && WorkPeriodTime.TotalMinutes > 1
                && AppServices.MainDataContext.IsCurrentWorkPeriodOpen;
        }

        private void OnEndOfDayExecute(string obj)
        {
            StopTimer();
            AppServices.MainDataContext.StopWorkPeriod(EndDescription);
            Refresh();
            AppServices.MainDataContext.CurrentWorkPeriod.PublishEvent(EventTopicNames.WorkPeriodStatusChanged);
            RuleExecutor.NotifyEvent(RuleEventNames.WorkPeriodEnds, new { WorkPeriod = AppServices.MainDataContext.CurrentWorkPeriod, UserName = AppServices.CurrentLoggedInUser.Name });
            InteractionService.UserIntraction.GiveFeedback(Resources.WorkPeriodEndsMessage);
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateNavigation);
        }

        private bool CanStartOfDayExecute(string arg)
        {
            return AppServices.ActiveAppScreen == AppScreens.WorkPeriods
                && (LastWorkPeriod == null || LastWorkPeriod.StartDate != LastWorkPeriod.EndDate);
        }

        private void OnStartOfDayExecute(string obj)
        {
            AppServices.MainDataContext.StartWorkPeriod(StartDescription, CashAmount, CreditCardAmount, TicketAmount);
            Refresh();
            AppServices.MainDataContext.CurrentWorkPeriod.PublishEvent(EventTopicNames.WorkPeriodStatusChanged);
            RuleExecutor.NotifyEvent(RuleEventNames.WorkPeriodStarts, new { WorkPeriod = AppServices.MainDataContext.CurrentWorkPeriod, UserName = AppServices.CurrentLoggedInUser.Name });
            InteractionService.UserIntraction.GiveFeedback(Resources.StartingWorkPeriodCompleted);
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateNavigation);
        }

        public void Refresh()
        {
            _workPeriods = null;
            if (LastWorkPeriod != null)
                WorkPeriodTime = new TimeSpan(DateTime.Now.Ticks - LastWorkPeriod.StartDate.Ticks);
            OpenTicketCount = Dao.Count<Ticket>(x => !x.IsPaid);

            if (OpenTicketCount > 0)
            {
                OpenTicketLabel = string.Format(Resources.ThereAreOpenTicketsWarning_f,
                                  OpenTicketCount);
            }
            else OpenTicketLabel = "";

            RaisePropertyChanged("WorkPeriods");
            RaisePropertyChanged("LastEndOfDayLabel");
            RaisePropertyChanged("WorkPeriods");
            RaisePropertyChanged("IsShutdownRequestVisible");
            RaisePropertyChanged("ConnectionCountLabel");

            StartDescription = "";
            EndDescription = "";
            CashAmount = 0;
            CreditCardAmount = 0;
            TicketAmount = 0;

            ActiveScreen = 0;

            CommandManager.InvalidateRequerySuggested();
        }
    }
}
