﻿using System;
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
            ReportContext.EmpScheduleEntries = null;
            FilterGroups.Add(CreateWorkPeriodFilterGroup());
        }

        protected override FlowDocument GetReport()
        {
            var currentPeriod = ReportContext.ThisWeekWorkPeriod; // ReportContext.CurrentWorkPeriod;

            var report = new SimpleReport("8cm");
            AddDefaultReportHeader(report, currentPeriod, Resources.PayrollReport);

            var table = new Dictionary<int, List<KeyValuePair<DateTime, DateTime>>>();
            var scheduledTable = new Dictionary<int, List<KeyValuePair<DateTime, DateTime>>>();
            foreach (var u in ReportContext.TimeCardEntries.Select(t => t.UserId).Distinct())
            {
                table.Add(u, new List<KeyValuePair<DateTime, DateTime>>());
                scheduledTable.Add(u, new List<KeyValuePair<DateTime, DateTime>>());
                foreach (var date in
                    EachDay(ReportContext.CurrentWorkPeriod.StartDate, ReportContext.CurrentWorkPeriod.EndDate))
                {
                    var es =
                        ReportContext.EmpScheduleEntries.Where(e => e.StartTime.Date.Equals(date.Date) && e.UserId == u);
                    if (es != null)
                    {
                        foreach (var e in es)
                        {
                            scheduledTable[u].Add(new KeyValuePair<DateTime, DateTime>(e.StartTime, e.EndTime));
                        }
                    }
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

                                DateTime end = ts[0 + i].DateTime;
                                table[u].Add(new KeyValuePair<DateTime, DateTime>(DateTime.MinValue, end));
                            }
                            else
                            {
                                DateTime end;
                                DateTime start = ts[0 + i].DateTime;

                                if (ts[1 + i].Action != 2)
                                {

                                    table[u].Add(new KeyValuePair<DateTime, DateTime>(start, DateTime.MinValue));
                                }
                                else
                                {
                                    end = ts[1 + i].DateTime;
                                   // minutes += (int) end.Subtract(start).TotalMinutes;
                                    table[u].Add(new KeyValuePair<DateTime, DateTime>(start, end));
                                }
                            }

                        }
                        else
                        {
                           
                            if (ts[0 + i].Action != 1)
                            {

                                DateTime end = ts[0 + i].DateTime;
                                table[u].Add(new KeyValuePair<DateTime, DateTime>(DateTime.MinValue, end));
                            }
                            else
                            {
                                DateTime start = ts[0 + i].DateTime;
                                table[u].Add(new KeyValuePair<DateTime, DateTime>(start, DateTime.MinValue));
                            }
                        }
                    }
                    
                   
                } 
            }

            

            foreach (var user in table.Keys)
            {
                var userInfo = new UserInfo { UserId = user };
                report.AddColumTextAlignment(userInfo.UserName, TextAlignment.Left, TextAlignment.Center, TextAlignment.Center, TextAlignment.Center);
                report.AddColumnLength(userInfo.UserName, "25*", "25*", "25*", "25*");

                report.AddTable(userInfo.UserName, userInfo.UserName, "", "", "");
                report.AddBoldRow(userInfo.UserName, "Date", "ClockIn", "ClockOut", "Time");
                var entries = table[user];
                var scheduleEntries = scheduledTable[user];
                int totalMinutes = 0;
                foreach (var entry in entries)
                {
                   
                   
                     var t1 = new TimeSpan(entry.Key.Ticks);
                     var t1Exist = !entry.Key.Equals(DateTime.MinValue);
                     var t2 = new TimeSpan(entry.Value.Ticks);
                     var t2Exist = !entry.Value.Equals(DateTime.MinValue);
                     var t3 = TimeSpan.MinValue;
                    var t3Exist = false;
                    if (t1Exist && t2Exist)
                    {
                        t3 = entry.Value.Subtract(entry.Key);                       
                        t3Exist = true;
                        if (t3.TotalMinutes < 1) //hack to discard seconds and still make display look good
                        {
                            t1 = t2;
                        }

                    }
                    var reportingDate = entry.Key;
                    if (entry.Key.Equals(DateTime.MinValue))
                    {
                        reportingDate = entry.Value;
                    }
                    report.AddRow(userInfo.UserName, reportingDate.Date.ToShortDateString(), 
                        t1Exist?(String.Format("{0:D2}:{1:D2}", t1.Hours, t1.Minutes)):"-----",
                        t2Exist?(String.Format("{0:D2}:{1:D2}", t2.Hours, t2.Minutes)):"-----",
                        t3Exist?(String.Format("{0:D2}:{1:D2}", t3.Hours, t3.Minutes)):"00:00");

                    foreach (var e in scheduleEntries)
                    {
                        if (e.Key.Date.Equals(reportingDate.Date))
                        {
                             var s1 = new TimeSpan(e.Key.Ticks);
                             var s1Exist = !e.Key.Equals(DateTime.MinValue);
                             var s2 = new TimeSpan(e.Value.Ticks);
                             var s2Exist = !e.Value.Equals(DateTime.MinValue);
                             var s3 = TimeSpan.MinValue;
                             var s3Exist = false;
                             if (s1Exist && s2Exist)
                             {
                                 s3 = entry.Value.Subtract(entry.Key);
                                 s3Exist = true;
                                 if (s3.TotalMinutes < 1) //hack to discard seconds and still make display look good
                                 {
                                     s1 = s2;
                                 }

                                 report.AddRow(userInfo.UserName, "Scheduled",
                                 s1Exist ? (String.Format("{0:D2}:{1:D2}", s1.Hours, s1.Minutes)) : "-----",
                                 s2Exist ? (String.Format("{0:D2}:{1:D2}", s2.Hours, s2.Minutes)) : "-----",
                                 s3Exist ? (String.Format("{0:D2}:{1:D2}", s3.Hours, s3.Minutes)) : "00:00");

                             }
                           
                            scheduleEntries.Remove(e);
                            break;
                        }
                    }
                   
                   

                    if (t3Exist)
                    {
                        totalMinutes += (int) t3.TotalMinutes;
                    }

                }
                var hours = totalMinutes/60;
                var minutes = totalMinutes%60; 
               
                report.AddBoldRow(userInfo.UserName, "Total Hours:", "", "",String.Format("{0:D2}:{1:D2}", hours, minutes));

                
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
