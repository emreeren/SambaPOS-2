using System;
using System.ComponentModel.Composition;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Tickets;
using Samba.Modules.CreditCardModule.Verifone;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;
using Samba.Services.Printing;

namespace Samba.Modules.CreditCardModule.Verifone
{
    /*
     * "gateway_id" => "AD1234-00", "password" => "password", "transaction_type" => "00", "amount" => "11", "cardholder_name" => "Test",
     * "cc_number" => "411111111111111", "cc_expiry" => "0314");
     */

    [Export(typeof(ICreditCardProcessor))]
    class VfCreditCardProcessor : ICreditCardProcessor
    {
        private readonly VfProcessorViewModel _viewModel;
        private VfProcessorView _view;
        private static readonly VfProcessorSettings Settings = new VfProcessorSettings();
        private readonly VerifoneOmni3750 _vfOmni = new VerifoneOmni3750();
        


        public Ticket SelectedTicket { get; set; }

        [ImportingConstructor]
        public VfCreditCardProcessor(IRegionManager regionManager, VfProcessorViewModel viewModel)
        {
            
            _viewModel = viewModel;
            _viewModel.Processed += ViewModelProcessed;
            Settings.Load();
          


        }

        public string Name
        {
            get { return "Verifone Credit Card Processor"; }
        }

        public void EditSettings()
        {
            InteractionService.UserIntraction.EditProperties(Settings);
            Settings.Save();
        }


        public bool ForcePayment(int ticketId)
        {
            return false;
        }


        public void Process(CreditCardProcessingData creditCardProcessingData)
        {
            
            InteractionService.UserIntraction.BlurMainWindow();
            SelectedTicket = creditCardProcessingData.Ticket;
            ReSetTagsForSelectedTicket();
            _viewModel.CanPreAuth = true;
            if (creditCardProcessingData.TenderedAmount > SelectedTicket.RemainingAmount)
            {
                MessageBox.Show(String.Format("Tendered Amount {0} can't be more than Balance Amount {1}.Resetting" +
                                              " tendered amount to balace amount",
                                              creditCardProcessingData.TenderedAmount,
                                              SelectedTicket.RemainingAmount));
                creditCardProcessingData.TenderedAmount = SelectedTicket.RemainingAmount;
            }
            else if (creditCardProcessingData.TenderedAmount == 0)
            {
                creditCardProcessingData.TenderedAmount = SelectedTicket.RemainingAmount;
            }
            
            _viewModel.TenderedAmount = creditCardProcessingData.TenderedAmount;
            _viewModel.Gratuity = (decimal) (creditCardProcessingData.TenderedAmount * Settings.GratuityRate) / 100;
            _view = new VfProcessorView(_viewModel);
           
            _viewModel.AuthCode = "";
            _view.ShowDialog();
            
            
        }

       

        public void ViewModelProcessed(object sender, OnProcessedArgs args)
        {
            var processType = args.ProcessType;
            var gratuity = _viewModel.Gratuity;
            var ticket = SelectedTicket;
           

            var result = new CreditCardProcessingResult { ProcessType = processType };
            var amount = _viewModel.TenderedAmount + gratuity;
            if (processType == ProcessType.External)
            {
                result.Amount = amount;
                SelectedTicket.SetTagValue("CC_TXTYPE", "External");
                InteractionService.UserIntraction.DeblurMainWindow();
                _view.Close();              
                result.PublishEvent(EventTopicNames.PaymentProcessed);
                return;
            }
           
            
          


            if (processType == ProcessType.Force)
            {

                _vfOmni.TerminalId = Settings.TerminalId;
                _vfOmni.Timeout = Settings.ComReadTimeout;
                _vfOmni.ComBaudRate = Settings.ComBaudRate;
                _vfOmni.ComPort = Settings.ComPort;
                _vfOmni.Retries = Settings.Retry;
                _vfOmni.LocalCurrency = Settings.LocalCurrency;
                _view.CardStatus.Text = "";
                _view.Refresh();



                TransactionResponse response;
                if (_vfOmni.SendTransactionRequest(amount, ticket.Id, out response))
                {


                    result.Amount = response.Amount;
                    result.ProcessType = ProcessType.Force;

                    SetTagsForSelectedTicket(result.Amount);
                   
                    _view.CardStatus.Text = "Successfully sent transaction.";
                    _view.Refresh();
                   
                   
                }
                else
                {
                    _view.CardStatus.Text = Localization.Properties.Resources.CreditCardRequestSendFailure + ". ";
                    _view.Refresh();
                    return;
                }

            }else if (processType == ProcessType.External)
            {
                result.Amount = amount;
               
            }
            InteractionService.UserIntraction.DeblurMainWindow();
            _view.Close();
            
            result.PublishEvent(EventTopicNames.PaymentProcessed);
        }

       

        void SetTagsForSelectedTicket(decimal amount)
        {
            
            SelectedTicket.SetTagValue("CC_AMOUNT", amount.ToString("#,#0.00"));           
            SelectedTicket.SetTagValue("CC_TXTYPE", "VF CC Purchase");         
        }
        void ReSetTagsForSelectedTicket()
        {
            SelectedTicket.SetTagValue("CC_AMOUNT", "");         
            SelectedTicket.SetTagValue("CC_TXTYPE", "");                    
        }
        
       
        
        
        

       
    }

}
