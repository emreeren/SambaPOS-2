using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Commands;
using Samba.Modules.CreditCardModule.FirstData;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.FirstData
{
    public class OnProcessedArgs
    {
        public ProcessType ProcessType { get; set; }
    }

   
    [Export]
    public class FdProcessorViewModel : ObservableObject
    {
        public delegate void OnProcessed(object sender, OnProcessedArgs args);

        
        public event OnProcessed Processed;

        private void InvokeProcessed(OnProcessedArgs args)
        {
            OnProcessed handler = Processed;
            if (handler != null) handler(this, args);
        }

        [ImportingConstructor]
        public FdProcessorViewModel()
        {
            ForceCommand = new DelegateCommand(OnForce);
            PreAuthCommand = new DelegateCommand(OnPreAuth, CanPreAuthExecute);
            CancelCommand = new DelegateCommand(OnCancel);
            SwipeCommand = new DelegateCommand(OnSwipe);
        }

        private bool CanPreAuthExecute()
        {
            return true;
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

        private void OnSwipe()
        {
            InvokeProcessed(new OnProcessedArgs { ProcessType = ProcessType.Swipe });
        }

        

       
        public DelegateCommand ForceCommand { get; set; }
        public DelegateCommand PreAuthCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }
        public DelegateCommand SwipeCommand { get; set; }
       

        public decimal TenderedAmount { get; set; }
        public decimal Gratuity { get; set; }
        public string AuthCode { get; set; }     
        public bool CanPreAuth { get; set; }
        

       
    }
}