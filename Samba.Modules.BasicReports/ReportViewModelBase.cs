﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using Microsoft.Win32;
using Samba.Domain.Models.Settings;
using Samba.Infrastructure.Settings;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ErrorReport;
using Samba.Services;

namespace Samba.Modules.BasicReports
{
    public abstract class ReportViewModelBase : ObservableObject
    {
        private readonly List<string> _links;
        public string Header { get { return GetHeader(); } }

        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                RaisePropertyChanged("Selected");
                RaisePropertyChanged("Background");
                RaisePropertyChanged("Foreground");
            }
        }

        public string Background { get { return Selected ? "Orange" : "White"; } }
        public string Foreground { get { return Selected ? "White" : "Black"; } }

        public ObservableCollection<FilterGroup> FilterGroups { get; set; }

        public string StartDateString { get { return ReportContext.StartDateString; } set { ReportContext.StartDateString = value; } }
        public string EndDateString { get { return ReportContext.EndDateString; } set { ReportContext.EndDateString = value; } }

        public ICaptionCommand PrintDocumentCommand { get; set; }
        public ICaptionCommand RefreshFiltersCommand { get; set; }
        public ICaptionCommand SaveDocumentCommand { get; set; }

        public FlowDocument Document { get; set; }

        public bool CanUserChangeDates { get { return AppServices.IsUserPermittedFor(PermissionNames.ChangeReportDate); } }

        protected ReportViewModelBase()
        {
            _links = new List<string>();
            PrintDocumentCommand = new CaptionCommand<string>(Resources.Print, OnPrintDocument);
            RefreshFiltersCommand = new CaptionCommand<string>(Resources.Refresh, OnRefreshFilters, CanRefreshFilters);
            SaveDocumentCommand = new CaptionCommand<string>(Resources.Save, OnSaveDocument);
            FilterGroups = new ObservableCollection<FilterGroup>();
        }

        private void OnSaveDocument(string obj)
        {
            var fn = AskFileName("Report", ".xps");
            if (!string.IsNullOrEmpty(fn))
            {
                try
                {
                    SaveAsXps(fn, Document);
                }
                catch (Exception e)
                {
                    AppServices.LogError(e);
                }
            }
        }

        public static void SaveAsXps(string path, FlowDocument document)
        {
            using (Package package = Package.Open(path, FileMode.Create))
            {
                using (var xpsDoc = new XpsDocument(
                    package, CompressionOption.Maximum))
                {
                    var xpsSm = new XpsSerializationManager(
                        new XpsPackagingPolicy(xpsDoc), false);
                    var dp = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    xpsSm.SaveAsXaml(dp);
                }
            }
        }

        public static void PrintReport(FlowDocument document)
        {
            AppServices.PrintService.PrintSlipReport(document);
        }

        internal string AskFileName(string defaultName, string extenstion)
        {
            defaultName = defaultName.Replace(" ", "_");
            defaultName = defaultName.Replace(".", "_");
            var saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = LocalSettings.DocumentPath,
                FileName = defaultName,
                Filter = string.Format("{0} file (*.{1})|*.{1}", extenstion.Trim('.').ToUpper(), extenstion.Trim('.')),
                DefaultExt = extenstion
            };

            var result = saveFileDialog.ShowDialog();
            return result.GetValueOrDefault(false) ? saveFileDialog.FileName : "";
        }

        public void HandleLink(string text)
        {
            if (!_links.Contains(text))
                _links.Add(text);
        }

        protected virtual void OnRefreshFilters(string obj)
        {
            var sw = FilterGroups[0].SelectedValue as WorkPeriod;
            if (sw == null) return;
            if (ReportContext.CurrentWorkPeriod != null && (ReportContext.StartDate != sw.StartDate || ReportContext.EndDate != sw.EndDate))
            {
                ReportContext.CurrentWorkPeriod =
                    ReportContext.CreateCustomWorkPeriod("", ReportContext.StartDate, ReportContext.EndDate);
            }
            else ReportContext.CurrentWorkPeriod =
                FilterGroups[0].SelectedValue as WorkPeriod;
            RefreshReport();
        }

        protected abstract void CreateFilterGroups();

        protected FilterGroup CreateWorkPeriodFilterGroup()
        {
            var wpList = ReportContext.GetWorkPeriods(ReportContext.StartDate, ReportContext.EndDate).ToList();
            wpList.Insert(0, ReportContext.ThisMonthWorkPeriod);
            wpList.Insert(0, ReportContext.LastMonthWorkPeriod);
            wpList.Insert(0, ReportContext.ThisWeekWorkPeriod);
            wpList.Insert(0, ReportContext.LastWeekWorkPeriod);
            wpList.Insert(0, ReportContext.YesterdayWorkPeriod);
            wpList.Insert(0, ReportContext.TodayWorkPeriod);

            if (!wpList.Contains(ReportContext.CurrentWorkPeriod))
            { wpList.Insert(0, ReportContext.CurrentWorkPeriod); }

            if (!wpList.Contains(AppServices.MainDataContext.CurrentWorkPeriod))
                wpList.Insert(0, AppServices.MainDataContext.CurrentWorkPeriod);

            return new FilterGroup { Values = wpList, SelectedValue = ReportContext.CurrentWorkPeriod };
        }

        private bool CanRefreshFilters(string arg)
        {
            return FilterGroups.Count > 0;
        }

        private void OnPrintDocument(string obj)
        {
            AppServices.PrintService.PrintSlipReport(Document);
        }

        public void RefreshReport()
        {
            Document = null;
            RaisePropertyChanged("Document");

            //Program ilk yüklendiğinde aktif gün başı işlemi yoktur.

            if (ReportContext.CurrentWorkPeriod == null) return;
            var memStream = new MemoryStream();
            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += delegate
                {
                    LocalSettings.UpdateThreadLanguage();
                    var doc = GetReport();
                    XamlWriter.Save(doc, memStream);
                    memStream.Position = 0;
                };

                worker.RunWorkerCompleted +=
                    delegate(object sender, RunWorkerCompletedEventArgs eventArgs)
                    {
                        if (eventArgs.Error != null)
                        {
                            ExceptionReporter.Show(eventArgs.Error);
                            return;
                        }

                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(
                        delegate
                        {
                            Document = (FlowDocument)XamlReader.Load(memStream);

                            foreach (var link in _links)
                            {
                                var hp = Document.FindName(link.Replace(" ", "_")) as Hyperlink;
                                if (hp != null) hp.Click += (s, e) => HandleClick(((Hyperlink)s).Name.Replace("_", " "));
                            }

                            RaisePropertyChanged("Document");
                            RaisePropertyChanged("StartDateString");
                            RaisePropertyChanged("EndDateString");

                            CreateFilterGroups();
                            foreach (var filterGroup in FilterGroups)
                            {
                                var group = filterGroup;
                                filterGroup.ValueChanged = delegate
                                                               {
                                                                   var sw = group.SelectedValue as WorkPeriod;
                                                                   if (sw != null)
                                                                   {
                                                                       ReportContext.StartDate = sw.StartDate;
                                                                       ReportContext.EndDate = sw.EndDate;
                                                                       RefreshFiltersCommand.Execute("");
                                                                   }
                                                               };
                            }
                        }));
                    };

                worker.RunWorkerAsync();
            }
        }

        protected abstract FlowDocument GetReport();
        protected abstract string GetHeader();

        protected virtual void HandleClick(string text)
        {
            // override if needed.
        }

        public FlowDocument GetReportDocument()
        {
            return GetReport();
        }

        public void AddDefaultReportHeader(SimpleReport report, WorkPeriod workPeriod, string caption)
        {
            report.AddHeader(AppServices.SettingService.CustomPosName);
            report.AddHeader(caption);
            if (workPeriod.EndDate > workPeriod.StartDate)
                report.AddHeader(workPeriod.StartDate.ToString("dd MMMM yyyy HH:mm") +
                    " - " + workPeriod.EndDate.ToString("dd MMMM yyyy HH:mm"));
            else
            {
                report.AddHeader(workPeriod.StartDate.ToString("dd MMMM yyyy HH:mm") +
                " - " + DateTime.Now.ToString("dd MMMM yyyy HH:mm"));
            }

            if (!string.IsNullOrEmpty(workPeriod.Description))
                report.AddHeader(workPeriod.Description);
        }
    }
}
