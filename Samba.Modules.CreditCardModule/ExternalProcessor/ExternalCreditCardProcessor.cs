using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Security;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.ExternalProcessor
{
    internal class PreauthData
    {
        public SecureString SwipeData { get; set; }
        public decimal TenderedAmount { get; set; }
        public decimal Gratuity { get; set; }
        public string MerchantAuthCode { get; set; }
    }

    [Export(typeof(ICreditCardProcessor))]
    class ExternalCreditCardProcessor : ICreditCardProcessor
    {
        private readonly ExternalProcessorViewModel _viewModel;
        private ExternalProcessorView _view;
        private static readonly ExternalProcessorSettings Settings = new ExternalProcessorSettings();
        private static readonly IDictionary<int, PreauthData> PreauthDataCache = new Dictionary<int, PreauthData>();

        public Ticket SelectedTicket { get; set; }

        [ImportingConstructor]
        public ExternalCreditCardProcessor(IRegionManager regionManager, ExternalProcessorViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.Processed += ViewModelProcessed;

            Settings.Load();

        }

        public string Name
        {
            get { return "External Credit Card Processor"; }
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
            _viewModel.CanPreAuth = !PreauthDataCache.ContainsKey(SelectedTicket.Id);
            _viewModel.TenderedAmount = creditCardProcessingData.TenderedAmount;
            _viewModel.Gratuity = (creditCardProcessingData.TenderedAmount * Settings.GratuityRate) / 100;
            _viewModel.AuthCode = "";
            _view = new ExternalProcessorView(_viewModel);
            _view.ShowDialog();
        }

        public bool ForcePayment(int ticketId)
        {
            return PreauthDataCache.ContainsKey(ticketId);
        }

        void ViewModelProcessed(object sender, OnProcessedArgs args)
        {
            var processType = args.ProcessType;
            var gratuity = _viewModel.Gratuity;
            var ticket = SelectedTicket;

            InteractionService.UserIntraction.DeblurMainWindow();
            _view.Close();

            var result = new CreditCardProcessingResult { ProcessType = processType };

            if (processType == ProcessType.PreAuth)
                result.Amount = Preauth(_view.SwipeDataBox.SecurePassword, ticket, _viewModel.TenderedAmount, gratuity);

            if (processType == ProcessType.Force)
                result.Amount = Force(_view.SwipeDataBox.SecurePassword, ticket, _viewModel.TenderedAmount, gratuity);

            result.PublishEvent(EventTopicNames.PaymentProcessed);
        }

        static void AddPreauthData(int ticketId, SecureString swipeData, string authCode, decimal tenderedAmount, decimal gratuity)
        {
            Debug.Assert(!PreauthDataCache.ContainsKey(ticketId));
            PreauthDataCache.Add(ticketId, new PreauthData
            {
                MerchantAuthCode = authCode,
                SwipeData = swipeData,
                TenderedAmount = tenderedAmount,
                Gratuity = gratuity
            });
        }

        static PreauthData GetPreauthData(int ticketId)
        {
            if (PreauthDataCache.ContainsKey(ticketId))
            {
                var result = PreauthDataCache[ticketId];
                PreauthDataCache.Remove(ticketId);
                return result;
            }
            return null;
        }

        private static decimal Force(SecureString swipeData, Ticket ticket, decimal tenderedAmount, decimal gratuity)
        {
            var result = tenderedAmount;
            if (!PreauthDataCache.ContainsKey(ticket.Id))
                result = Preauth(swipeData, ticket, tenderedAmount, gratuity);
            ForceWithPreauth(ticket.Id);
            return result;
        }

        private static void ForceWithPreauth(int ticketId)
        {
            // Force preauth payment
            Debug.Assert(PreauthDataCache.ContainsKey(ticketId));
            var preauthData = GetPreauthData(ticketId);
            using (var sm = new SecureStringToStringMarshaler(preauthData.SwipeData))
            {
                // access swipedata as demonstrated here 
                InteractionService.UserIntraction.GiveFeedback("Force:\r" + sm.String);
                // *------------------------
                // force with preauth data;
                // *------------------------
            }
            preauthData.SwipeData.Clear(); // we don't need swipedata anymore...
        }

        private static decimal Preauth(SecureString swipeData, Ticket ticket, decimal tenderedAmount, decimal gratuity)
        {
            // preauthPayment

            if (gratuity > 0 && Settings.GratuityService != null) // add gratuity amount to ticket
                ticket.AddTaxService(Settings.GratuityService.Id, Settings.GratuityService.CalculationMethod, gratuity);

            using (var sm = new SecureStringToStringMarshaler(swipeData))
            {
                // access swipedata as demonstrated here 
                InteractionService.UserIntraction.GiveFeedback(string.Format("Amount:{0}\r\rPreauth:\r{1}", ticket.GetRemainingAmount(), sm.String));
                // *------------------------
                // Preauth Here
                // *------------------------
            }
            AddPreauthData(ticket.Id, swipeData, "SAMPLE MERCHANT AUTH CODE", tenderedAmount, gratuity);
            return tenderedAmount + gratuity;
        }
    }

}
