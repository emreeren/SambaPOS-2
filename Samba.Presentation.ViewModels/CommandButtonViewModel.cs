using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Samba.Domain.Models.Settings;
using Samba.Presentation.Common;

namespace Samba.Presentation.ViewModels
{
    public class CommandButtonViewModel : ObservableObject
    {
        public ICaptionCommand Command { get; set; }

        private string _caption;
        public string Caption
        {
            get { return _caption; }
            set { _caption = value; RaisePropertyChanged("Caption"); }
        }

        public PrintJob Parameter { get; set; }
    }
}
