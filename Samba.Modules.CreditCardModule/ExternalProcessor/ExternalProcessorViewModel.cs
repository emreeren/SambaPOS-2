using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Commands;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.ExternalProcessor
{
    public class OnProcessedArgs
    {
        public ProcessType ProcessType { get; set; }
    }

    [Export]
    public class ExternalProcessorViewModel : ObservableObject
    {
        public delegate void OnProcessed(object sender, OnProcessedArgs args);
        public event OnProcessed Processed;

        private void InvokeProcessed(OnProcessedArgs args)
        {
            OnProcessed handler = Processed;
            if (handler != null) handler(this, args);
        }

        [ImportingConstructor]
        public ExternalProcessorViewModel()
        {
            ForceCommand = new DelegateCommand(OnForce);
            PreAuthCommand = new DelegateCommand(OnPreAuth, CanPreAuthExecute);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        private bool CanPreAuthExecute()
        {
            return CanPreAuth;
        }

        private void OnPreAuth()
        {
            var args = new OnProcessedArgs { ProcessType = ProcessType.PreAuth };
            InvokeProcessed(args);
        }

        private void OnCancel()
        {
            InvokeProcessed(new OnProcessedArgs { ProcessType = ProcessType.Cancel });
        }

        private void OnForce()
        {
            InvokeProcessed(new OnProcessedArgs { ProcessType = ProcessType.Force });
        }

        public DelegateCommand PreAuthCommand { get; set; }
        public DelegateCommand ForceCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }

        public decimal TenderedAmount { get; set; }
        public decimal Gratuity { get; set; }
        public string AuthCode { get; set; }
        public string CardholderName { get; set; }
        public bool CanPreAuth { get; set; }
    }

}
