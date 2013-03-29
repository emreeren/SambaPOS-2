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
            FilterGroups.Add(CreateWorkPeriodFilterGroup());
        }

        protected override FlowDocument GetReport()
        {
            var currentPeriod = ReportContext.CurrentWorkPeriod;

            var report = new SimpleReport("8cm");
            AddDefaultReportHeader(report, currentPeriod, Resources.PayrollReport);

             var table = new Dictionary<int, Dictionary<DateTime, int>>();
            foreach (var u in ReportContext.TimeCardEntries.Select(t => t.UserId).Distinct())
            {
                table.Add(u, new Dictionary<DateTime, int>());
                foreach (var date in
                    EachDay(ReportContext.CurrentWorkPeriod.StartDate, ReportContext.CurrentWorkPeriod.EndDate))
                {
                    int minutes = 0;
                    bool error = false;
                    var ts = ReportContext.TimeCardEntries.Where(t => t.DateTime.Date.Equals(date.Date) && t.UserId == u).ToArray();

                    for (int i = 0; i < ts.Count(); i = i + 2)
                    {
                       
                       
                        if (ts.Count() > i+1)
                        {
                            if (ts[0+i].Action != 1)
                            {
                                error = true;
                            }
                            else
                            {
                                DateTime end;
                                DateTime start = ts[0 + i].DateTime;

                                if (ts[1+i].Action != 2)
                                {
                                    error = true;
                                }
                                else
                                {
                                    end = ts[1+i].DateTime;
                                    minutes += (int)end.Subtract(start).TotalMinutes;
                                }
                            }

                        }
                    }
                    if (error)
                    {
                        minutes = 0;
                    }
                    table[u].Add(date.Date,minutes);
                } 
            }

            

            foreach (var user in table.Keys)
            {
                var userInfo = new UserInfo { UserId = user };
                report.AddColumTextAlignment("Garson", TextAlignment.Left, TextAlignment.Right);
                report.AddColumnLength("Garson", "65*", "35*");

                report.AddTable(userInfo.UserName,  userInfo.UserName, "");
                report.AddBoldRow(userInfo.UserName, "Date", "Minutes");
                var entries = table[user];
                decimal totalMinutes = 0;
                foreach (var entry in entries)
                {
                    totalMinutes += entry.Value;
                    report.AddRow(userInfo.UserName, entry.Key.Date.ToShortDateString(),entry.Value);
                }
                report.AddBoldRow(userInfo.UserName, "Total Hours:", totalMinutes/60);
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
