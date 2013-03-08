using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Practices.Prism.Regions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;

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
        private static readonly IDictionary<int, PreauthData> PreauthDataCache = new Dictionary<int, PreauthData>();

       

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
            _viewModel.CanPreAuth = !PreauthDataCache.ContainsKey(SelectedTicket.Id);
            _viewModel.TenderedAmount = creditCardProcessingData.TenderedAmount;
            _viewModel.Gratuity = (creditCardProcessingData.TenderedAmount * Settings.GratuityRate) / 100;
                    
            _viewModel.AuthCode = "";
            _view = new FdProcessorView(_viewModel);
            _view.ShowDialog();
        }

        public bool ForcePayment(int ticketId)
        {
            return PreauthDataCache.ContainsKey(ticketId);
        }

       
        public void ViewModelProcessed(object sender, OnProcessedArgs args)
        {
            var processType = args.ProcessType;
            var gratuity = _viewModel.Gratuity;
            var ticket = SelectedTicket;

           if (processType == ProcessType.Swipe)
           {
               var ccData = ReadCreditCardTrackData();
               if (ccData == null)
               {
                   _view.CardStatus.Text = "Failed to read Credit Card information. Please try again.";
                   _view.Refresh();
                   return;
               }
               _view.CardExpire.Text = ccData.CardExpiry;
               _view.CardName.Text = ccData.CardName;
               _view.CardNumber.Password = ccData.CardNumber;
               _view.CardStatus.Text = "Successfully read the card";
               _view.Refresh();
               return;
           }
           

            var result = new CreditCardProcessingResult { ProcessType = processType };



            if (processType == ProcessType.Force)
            {
                
                var amount = _viewModel.TenderedAmount + gratuity;
                string requestStatus;
               
                var resp = Force(ticket, amount, out requestStatus);
                if (resp != null)
                {
                    //rjoshi fix me
                    AppServices.PrintService.PrintSlipReport(new FlowDocument(new Paragraph(new Run(resp.ctr))));
                    if (resp.transaction_approved)
                    {
                        result.Amount = amount;
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
                    _view.CardStatus.Text = "Failed to send request. " + requestStatus;
                    _view.Refresh();
                    return;
                }



            }
            InteractionService.UserIntraction.DeblurMainWindow();
            _view.Close();
            result.PublishEvent(EventTopicNames.PaymentProcessed);
        }

       
        private FdCreditCardResp Force(Ticket ticket, decimal amount, out string requestStatus)
        {
            FdCreditCardReq ccinfo = new FdCreditCardReq();
            ccinfo.amount = amount.ToString();
            ccinfo.cardholder_name = _view.CardName.Text;
            ccinfo.cc_expiry = _view.CardExpire.Text;
            ccinfo.cc_number = _view.CardNumber.Password;
            ccinfo.gateway_id = Settings.GatewayId;
            ccinfo.password = Settings.Password;
            ccinfo.transaction_type = "00";
            string jsonPayload = JsonConvert.SerializeObject(ccinfo);
            var byteArray = Encoding.UTF8.GetBytes(jsonPayload);
            try
            {
                var req = (HttpWebRequest) WebRequest.Create(Settings.GatewayUri);
                req.Method = "POST";
                req.ContentType = "application/json; charset=utf-8";
                req.Accept = "application/json";
                req.Proxy = null;
              //  req.Timeout = 5000;
               // req.KeepAlive = true;

                /*
                string gge4_date = DateTime.UtcNow.ToString("%Y-%m-%dT%H:%M:%S") + 'Z';
   
               
                string fpSequence = new Random().Next().ToString();
                var time = (DateTime.UtcNow - new DateTime(1970, 1, 1));;
                int timeStamp = (int)time.TotalSeconds;

                string hmacData = String.Format("{0}^{1}^{2}^{3}^{4}", Settings.LoginId, fpSequence,
                                                timeStamp, amount, Settings.CurrencyCode);
                HMACMD5 hmac = new HMACMD5(Encoding.ASCII.GetBytes(Settings.HmacKey));
                byte[] hmacBuffer = Encoding.ASCII.GetBytes(hmacData);
                MemoryStream hmacStream = new MemoryStream(hmacBuffer);
               
                string hash = System.Convert.ToBase64String( hmac.ComputeHash(hmacStream));
                */

                req.ContentLength = byteArray.Length;
                Stream dataStream = req.GetRequestStream();
                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);
                // Close the Stream object.
                dataStream.Close();
                //var task = MakeAsyncRequest(req);
               
                WebResponse response = req.GetResponse();
                requestStatus = ((HttpWebResponse) response).StatusDescription;
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
               
                FdCreditCardResp ccResp;
                try
                {
                    ccResp = JsonConvert.DeserializeObject<FdCreditCardResp>(responseFromServer);
                }
                catch (Exception ex)
                {
                    JObject jobj = JObject.Parse(responseFromServer);

                     ccResp = new FdCreditCardResp();
                    ccResp.transaction_approved = (jobj["transaction_approved"].ToString() == "1") ? true : false;
                    ccResp.bank_message = jobj["bank_message"].ToString();
                    ccResp.exact_resp_code = jobj["exact_resp_code"].ToString();

                }

                requestStatus = ccResp.bank_message;
                return ccResp;
            }
            catch (Exception ex)
            {
                requestStatus =  ex.Message;
                return null;
            }
        }
        public static Task<string> MakeAsyncRequest(HttpWebRequest request)
        {
           
            Task<WebResponse> task = Task.Factory.FromAsync(
                request.BeginGetResponse,
                asyncResult => request.EndGetResponse(asyncResult),
                (object)null);

            return task.ContinueWith(t => ReadStreamFromResponse(t.Result));
        }

        private static string ReadStreamFromResponse(WebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader sr = new StreamReader(responseStream))
            {
                //Need to return this response 
                string strContent = sr.ReadToEnd();
                return strContent;
            }
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
            AddPreauthData(ticket.Id, swipeData, "FIRST Data MERCHANT AUTH CODE", tenderedAmount, gratuity);
            return tenderedAmount + gratuity;
        }

        protected CreditCardTrackData ReadCreditCardTrackData()
        {
            // _viewModel.CardExpiry = creditCardProcessingData.CardExpiry;
            // _viewModel.CardName = creditCardProcessingData.CardName;
            //  _viewModel.CardNumber = creditCardProcessingData.CardNumber;
            string track1 = SerialPortService.ReadLineFromPort("COM4");
            if (!String.IsNullOrEmpty(track1))
            {
                var trackData = ParseSwipeData(track1);
                if (trackData != null)
                {
                    return trackData;
                }
            }

            string track2 = SerialPortService.ReadLineFromPort("COM4");
            if (!String.IsNullOrEmpty(track2))
            {
                var trackData = ParseSwipeData(track2);
                if (trackData != null)
                {
                    return trackData;
                }
            }
            return null;
        }
        private CreditCardTrackData ParseSwipeData(string swipeData)
        {
            bool CaretPresent = false;
            bool EqualPresent = false;

            CaretPresent = swipeData.Contains("^");
            EqualPresent = swipeData.Contains("=");

            if (CaretPresent)
            {
                CreditCardTrackData ccTrack = new CreditCardTrackData();
                string[] CardData = swipeData.Split('^');
                //B1234123412341234^CardUser/John^030510100000019301000000877000000?

                ccTrack.CardName = FormatName(CardData[1]);
                ccTrack.CardNumber = FormatCardNumber(CardData[0]);
                ccTrack.CardExpiry= CardData[2].Substring(2, 2) +  CardData[2].Substring(0, 2);

                return ccTrack;
            }
            else if (EqualPresent)
            {
                string[] CardData = swipeData.Split('=');
                //1234123412341234=0305101193010877?
                CreditCardTrackData ccTrack = new CreditCardTrackData();
                ccTrack.CardNumber = FormatCardNumber(CardData[0]);
                ccTrack.CardExpiry = CardData[1].Substring(2, 2) +  CardData[1].Substring(0, 2);

                return ccTrack;
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

                result = NameSplit[1] + " " + NameSplit[0];
            }
            else
            {
                result = o;
            }

            return result;
        }
    }

}
