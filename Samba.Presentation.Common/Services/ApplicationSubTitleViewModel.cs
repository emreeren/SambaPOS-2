namespace Samba.Presentation.Common.Services
{
    public class ApplicationSubTitleViewModel : ObservableObject
    {
        public ApplicationSubTitleViewModel()
        {
            ApplicationTitleColor = "White";
            ApplicationTitleFontSize = 24;
        }

        private string _applicationTitle;
        public string ApplicationTitle
        {
            get { return _applicationTitle; }
            set
            {
                _applicationTitle = value;
                RaisePropertyChanged("ApplicationTitle");
            }
        }

        private int _applicationTitleFontSize;
        public int ApplicationTitleFontSize
        {
            get { return _applicationTitleFontSize; }
            set
            {
                _applicationTitleFontSize = value;
                RaisePropertyChanged("ApplicationTitleFontSize");
            }
        }

        private string _applicationTitleColor;
        public string ApplicationTitleColor
        {
            get { return _applicationTitleColor; }
            set
            {
                _applicationTitleColor = value;
                RaisePropertyChanged("ApplicationTitleColor");
            }
        }
    }
}