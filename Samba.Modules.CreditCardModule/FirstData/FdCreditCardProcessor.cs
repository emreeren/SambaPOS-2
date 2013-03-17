using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Practices.Prism.Regions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;
using Samba.Services.Printing;

namespace Samba.Modules.CreditCardModule.FirstData
{
    /*
     * "gateway_id" => "AD1234-00", "password" => "password", "transaction_type" => "00", "amount" => "11", "cardholder_name" => "Test",
     * "cc_number" => "411111111111111", "cc_expiry" => "0314");
     */

    [Export(typeof(ICreditCardProcessor))]
    class FdCreditCardProcessor : ICreditCardProcessor
    {
        private readonly FdProcessorViewModel _viewModel;
        private FdProcessorView _view;
        private static readonly FdProcessorSettings Settings = new FdProcessorSettings();


        public Ticket SelectedTicket { get; set; }

        [ImportingConstructor]
        public FdCreditCardProcessor(IRegionManager regionManager, FdProcessorViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.Processed += ViewModelProcessed;
            Settings.Load();
        }

        public string Name
        {
            get { return "First Data Credit Card Processor"; }
        }

        public void EditSettings()
        {
            InteractionService.UserIntraction.EditProperties(Settings);
            Settings.Save();
        }

        public void Process(CreditCardProcessingData creditCardProcessingData)
        {
            InteractionService.UserIntraction.BlurMainWindow();
            SelectedTicket = creditCardProcessingData.Ticket;
            _viewModel.CanPreAuth = true;
            _viewModel.TenderedAmount = creditCardProcessingData.TenderedAmount;
            _viewModel.Gratuity = (creditCardProcessingData.TenderedAmount * Settings.GratuityRate) / 100;
            _view = new FdProcessorView(_viewModel);
            _view.FdTransactionType.SelectedIndex = 0;
            _viewModel.AuthCode = "";
            _view.ShowDialog();
        }

        public bool ForcePayment(int ticketId)
        {
            return false;
        }

        public void ViewModelProcessed(object sender, OnProcessedArgs args)
        {
            var processType = args.ProcessType;
            var gratuity = _viewModel.Gratuity;
            var ticket = SelectedTicket;
            var txType = ((ComboBoxItem)_view.FdTransactionType.SelectedItem).Tag;


            var result = new CreditCardProcessingResult { ProcessType = processType };
            var amount = _viewModel.TenderedAmount + gratuity;
            if (processType == ProcessType.External)
            {
                result.Amount = amount;
                InteractionService.UserIntraction.DeblurMainWindow();
                _view.Close();
                result.PublishEvent(EventTopicNames.PaymentProcessed);
                return;
            }
           
            
           if (processType == ProcessType.Swipe)
           {
               string debugTrack = "";
               _view.CardStatus.Text = Samba.Localization.Properties.Resources.SwipeCreditCard;
               _view.Refresh();
               var ccData = ReadCreditCardTrackData(out debugTrack);
               if (ccData == null)
               {
                   if (String.IsNullOrWhiteSpace(debugTrack))
                   {
                       _view.CardStatus.Text = Samba.Localization.Properties.Resources.CreditCardReadFailed;
                   }
                   else
                   {
                       _view.CardStatus.Text = debugTrack;
                   }
                   _view.Refresh();
                   return;
               }
               
               _view.CardExpire.Text = ccData.CardExpiry;
               _view.CardName.Text = ccData.CardName;
               _view.CardNumber.Password = ccData.CardNumber;
               _view.CardStatus.Text = Samba.Localization.Properties.Resources.CreditCardReadSuccess;
               _view.Refresh();
               return;
           }
           

          
            if (processType == ProcessType.Force)
            {
                              
                string requestStatus;
                _view.CardStatus.Text = "";
                _view.Refresh();

                var resp = Force(ticket, amount, out requestStatus);
                if (resp != null)
                {
                    //rjoshi fix me

                    var content = resp.ctr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    PrintJobFactory.CreatePrintJob(AppServices.CurrentTerminal.SlipReportPrinter).DoPrint(content);

                    if (resp.transaction_approved)
                    {
                        result.Amount = amount;
                        _view.CardStatus.Text = resp.bank_message; 

                        //TODO Print job
                    }
                    else
                    {
                        _view.CardStatus.Text = resp.bank_message;
                        _view.Refresh();
                        return;
                    }
                }
                else
                {
                    _view.CardStatus.Text = Localization.Properties.Resources.CreditCardRequestSendFailure + " " + requestStatus;
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


        private FdCreditCardResp Force(Ticket ticket, decimal amount, out string requestStatus)
        {
            var txType = ((ComboBoxItem)_view.FdTransactionType.SelectedItem).Tag.ToString();
            FdCreditCardReq fdReq = new FdCreditCardReq(txType);
            fdReq.amount = amount.ToString();
            fdReq.cardholder_name = _view.CardName.Text;
            fdReq.cc_expiry = _view.CardExpire.Text;
            fdReq.cc_number = _view.CardNumber.Password;
            fdReq.gateway_id = Settings.GatewayId;
            fdReq.password = Settings.Password;
            fdReq.transaction_type = txType;
            fdReq.reference_no = ticket.Id.ToString();

            FdCreditCardResp fdResp;
            var fdGwMgr = new FdGatewayManager(Settings.GatewayUri, FdGatewayManager.ApiVersion.V11);
            if (fdGwMgr.SendFdCreditCardRequest(fdReq, out fdResp, out requestStatus))
            {

                return fdResp;
            }
            else
            {
                return null;
            }
        }
       
        
        /// <summary>
        /// Read Credit card data
        /// </summary>
        /// <param name="trackDebug"></param>
        /// <returns></returns>
        protected CreditCardTrackData ReadCreditCardTrackData(out string trackDebug)
        {
            
            string error = "";
            string tracks = SerialPortService.ReadExisting(Settings.ComPort,Settings.ComBaudRate,  Settings.ComReadTimeout, ref error);
            if (!String.IsNullOrEmpty(tracks))
            {
                trackDebug = "Track1:" + tracks;
                var trackData = ParseSwipeData(tracks);
                if (trackData != null)
                {
                    return trackData;
                }
            }
            trackDebug = "Track1:" + error;
           
            return null;
        }
        private CreditCardTrackData ParseSwipeData(string swipeData)
        {
            bool CaretPresent = false;
            bool EqualPresent = false;

            

            var tracks = swipeData.Split(new char[] {'?'});

            if (tracks == null || tracks.Length == 0)
            {
                return null;
            }

            foreach (var track in tracks)
            {


                CaretPresent = swipeData.Contains("^");
                EqualPresent = swipeData.Contains("=");

                if (CaretPresent)
                {
                    CreditCardTrackData ccTrack = new CreditCardTrackData();
                    string[] CardData = swipeData.Split('^');
                    //%B1234123412341234^CardUser/John^030510100000019301000000877000000?

                    ccTrack.CardName = FormatName(CardData[1]);                 
                    ccTrack.CardNumber = FormatCardNumber(CardData[0]);
                    ccTrack.CardExpiry = CardData[2].Substring(2, 2) + CardData[2].Substring(0, 2);
                    ccTrack.ServiceCode = CardData[2].Substring(4, 3);
                    ccTrack.DiscretionaryData = CardData[2].Substring(7);
                    return ccTrack;
                }
                 if(EqualPresent)
                {
                   
                        string[] CardData = swipeData.Split('=');
                        //;1234123412341234=0305101 193010877?
                        CreditCardTrackData ccTrack = new CreditCardTrackData();
                        ccTrack.CardNumber = FormatCardNumber(CardData[0].Substring(1));
                        ccTrack.CardExpiry = CardData[1].Substring(2, 2) + CardData[1].Substring(0, 2);
                        ccTrack.ServiceCode = CardData[1].Substring(4, 3);
                        ccTrack.DiscretionaryData = CardData[1].Substring(7);

                        return ccTrack;
                    }
                }
            
            return null;
        }

        private string FormatCardNumber(string o)
        {
            string result = string.Empty;

            result = Regex.Replace(o, "[^0-9]", string.Empty);

            return result;
        }

        private string FormatName(string o)
        {
            string result = string.Empty;

            if (o.Contains("/"))
            {
                string[] NameSplit = o.Split('/');

                result = NameSplit[1].Trim() + " " + NameSplit[0].Trim();
               
            }
            else
            {
                result = o;
            }

            return result;
        }
    }

}
