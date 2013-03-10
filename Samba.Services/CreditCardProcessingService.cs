
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using Samba.Domain;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Settings;

namespace Samba.Services
{
    public enum ProcessType
    {
        PreAuth,
        Force,
        Cancel,
        Swipe,
        External //external processing. system assume payment received
    }

    public class CreditCardProcessingResult
    {
        public ProcessType ProcessType { get; set; }
        public decimal Amount { get; set; }
        public string CardTxResponse { get; set; }
    }

    public class CreditCardProcessingData
    {
        public Ticket Ticket { get; set; }
        public decimal TenderedAmount { get; set; }
       
       

    }

    public class CreditCardTrackData
    {
        public string CardName { get; set; }
        public string CardExpiry { get; set; }
        public String CardNumber { get; set; }
        public string ServiceCode { get; set; }
        public string DiscretionaryData { get; set; }
    }

    public interface ICreditCardProcessor
    {
        string Name { get; }
        void EditSettings();
        void Process(CreditCardProcessingData creditCardProcessingData);
        bool ForcePayment(int ticketId);
        
    }

    public static class CreditCardProcessingService
    {
        private static IList<ICreditCardProcessor> CreditCardProcessors { get; set; }

        static CreditCardProcessingService()
        {
            CreditCardProcessors = new List<ICreditCardProcessor>();
        }

        public static void RegisterCreditCardProcessor(ICreditCardProcessor processor)
        {
            CreditCardProcessors.Add(processor);
        }

        public static IEnumerable<ICreditCardProcessor> GetProcessors()
        {
            return CreditCardProcessors;
        }

        public static ICreditCardProcessor GetDefaultProcessor()
        {
            var processorName = LocalSettings.DefaultCreditCardProcessorName;
            var result = CreditCardProcessors.FirstOrDefault(x => x.Name == processorName);
            return result;
        }

        public static bool CanProcessCreditCards { get { return GetDefaultProcessor() != null; } }

        public static void Process(CreditCardProcessingData ccpd)
        {
            GetDefaultProcessor().Process(ccpd);
        }

        public static bool ForcePayment(int ticketId)
        {
            return (CanProcessCreditCards && GetDefaultProcessor().ForcePayment(ticketId));
        }

      
    }
}
