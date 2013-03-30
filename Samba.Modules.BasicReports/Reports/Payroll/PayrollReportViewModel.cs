using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;

namespace Samba.Modules.BasicReports.Reports.Payroll
{
    public class PayrollReportViewModel : ReportViewModelBase
    {
        protected override void CreateFilterGroups()
        {
            FilterGroups.Clear();
            ReportContext.TimeCardEntries = null;
            FilterGroups.Add(CreateWorkPeriodFilterGroup());
        }

        protected override FlowDocument GetReport()
        {
            var currentPeriod = ReportContext.CurrentWorkPeriod;

            var report = new SimpleReport("8cm");
            AddDefaultReportHeader(report, currentPeriod, Resources.PayrollReport);

             var table = new Dictionary<int, Dictionary<DateTime, KeyValuePair<int, string>>>();
            foreach (var u in ReportContext.TimeCardEntries.Select(t => t.UserId).Distinct())
            {
                table.Add(u, new Dictionary<DateTime, KeyValuePair<int, string>>());
                foreach (var date in
                    EachDay(ReportContext.CurrentWorkPeriod.StartDate, ReportContext.CurrentWorkPeriod.EndDate))
                {
                    int minutes = 0;
                    
                    string errMsg = "";
                    var ts = ReportContext.TimeCardEntries.Where(t => t.DateTime.Date.Equals(date.Date) && t.UserId == u).ToArray();
                    if (!ts.Any())
                    {
                        continue;
                    }

                    for (int i = 0; i < ts.Count(); i = i + 2)
                    {
                       
                        if (ts.Count() > i + 1)
                        {
                            if (ts[0 + i].Action != 1)
                            {
                                
                                errMsg = ", No ClockIn";
                            }
                            else
                            {
                                DateTime end;
                                DateTime start = ts[0 + i].DateTime;

                                if (ts[1 + i].Action != 2)
                                {
                                   
                                    errMsg = ", No ClockOut";
                                }
                                else
                                {
                                    end = ts[1 + i].DateTime;
                                    minutes += (int) end.Subtract(start).TotalMinutes;
                                }
                            }

                        }
                        else
                        {
                           
                            if (ts[0 + i].Action != 1)
                            {

                                errMsg = ", No ClockIn";
                            }
                            else
                            {
                                errMsg = ", No ClockOut";
                            }
                        }
                    }
                    
                    table[u].Add(date.Date,new KeyValuePair<int, string>(minutes, errMsg));
                } 
            }

            

            foreach (var user in table.Keys)
            {
                var userInfo = new UserInfo { UserId = user };
                report.AddColumTextAlignment("Garson", TextAlignment.Left, TextAlignment.Right);
                report.AddColumnLength("Garson", "65*", "35*");

                report.AddTable(userInfo.UserName,  userInfo.UserName, "");
                report.AddBoldRow(userInfo.UserName, "Date", "Time");
                var entries = table[user];
                int totalMinutes = 0;
                foreach (var entry in entries)
                {
                     totalMinutes += entry.Value.Key;
                     var t = new TimeSpan(0, entry.Value.Key, 0);
                     report.AddRow(userInfo.UserName, entry.Key.Date.ToShortDateString(), String.Format("{0:D2}:{1:D2}{2}", t.Hours , t.Minutes, entry.Value.Value));
                    
                }
                var total = new TimeSpan(0, totalMinutes, 0);
                report.AddBoldRow(userInfo.UserName, "Total Hours:", String.Format("{0:D2}:{1:D2}", total.Hours, total.Minutes));
            }
                   
            return report.Document;
        }

        protected override string GetHeader()
        {
            return Resources.PayrollReport;
        }

        public IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }
    }
}
