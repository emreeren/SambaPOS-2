﻿using System.Collections.Generic;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Settings
{
    public class Terminal : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] LastUpdateTime { get; set; }
        public bool IsDefault { get; set; }
        public bool AutoLogout { get; set; }
        public bool HideExitButton { get; set; }
        public virtual Printer SlipReportPrinter { get; set; }
        public virtual Printer ReportPrinter { get; set; }
        public int DepartmentId { get; set; }
        public bool DisableMultipleItemSelection { get; set; }
       

        private IList<PrintJob> _printJobs;
        public virtual IList<PrintJob> PrintJobs
        {
            get { return _printJobs; }
            set { _printJobs = value; }
        }

        private static readonly Terminal _defaultTerminal = new Terminal { Name = "Varsayılan Terminal" };
        public static Terminal DefaultTerminal { get { return _defaultTerminal; } }

        public Terminal()
        {
            _printJobs = new List<PrintJob>();
        }
    }
}
