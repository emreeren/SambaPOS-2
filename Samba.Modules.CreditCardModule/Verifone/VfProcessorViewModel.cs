using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Commands;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.Verifone
{
    public class OnProcessedArgs
    {
        public ProcessType ProcessType { get; set; }
    }

   
    [Export]
    public class VfProcessorViewModel : ObservableObject
    {
        public delegate void OnProcessed(object sender, OnProcessedArgs args);

        
        public event OnProcessed Processed;

        private void InvokeProcessed(OnProcessedArgs args)
        {
            OnProcessed handler = Processed;
            if (handler != null) handler(this, args);
        }

        [ImportingConstructor]
        public VfProcessorViewModel()
        {
            ForceCommand = new DelegateCommand(OnForce);
            PreAuthCommand = new DelegateCommand(OnPreAuth, CanPreAuthExecute);
            CancelCommand = new DelegateCommand(OnCancel);
           
            ExternalCommand = new DelegateCommand(OnExternal);
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

       

        private void OnExternal()
        {
            InvokeProcessed(new OnProcessedArgs { ProcessType = ProcessType.External });
        }



        public DelegateCommand ExternalCommand { get; set; }
        public DelegateCommand ForceCommand { get; set; }
        public DelegateCommand PreAuthCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }
       
       

        public decimal TenderedAmount { get; set; }
        public decimal Gratuity { get; set; }
        public string AuthCode { get; set; }     
        public bool CanPreAuth { get; set; }
        

       
    }
}