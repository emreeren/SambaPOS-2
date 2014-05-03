using System;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Globalization;
using System.ComponentModel;

namespace CGeers.Cardfon
{
    internal enum TwoStepProtocol
    {
        Timeout = 0x0,
        StartOfText = 0x2, 
        EndOfText = 0x3,
        EndOfTransmission = 0x4,
        Enquiry = 0x5,
        Acknowledge = 0x6,
        NegativeAcknowledge = 0x15
    }

    public enum MessageType
    {
        [StringValue("00C1")]
        TransactionRequest = 0,
        [StringValue("00D1")]
        TransactionResponse = 1
    }

    internal enum TransactionType
    {
        [StringValue("00")]
        Purchase = 0
    }

    public enum AdditionalInformationType
    {
        [StringValue("01")]
        Currency = 0,
        [StringValue("02")]
        PetrolProductInformation = 1,
        [StringValue("03")]
        SpecialProductInformation = 2
    }

    public enum TransactionResult
    {
        [StringValue("0")]
        Ok = 0,
        [StringValue("1")]
        NotOk = 1,
        [StringValue("2")]
        ValueWrong = 2,
        [StringValue("3")]
        IncorrectProductInfo = 3,
        [StringValue("4")]
        PriceLitreCheckFailed  = 4,
        [StringValue("5")]
        TotalAmountNotEqualToSumOfSubtotal = 5,
        [StringValue("6")]
        SyntaxErrorTransactionFailed = 6,
        [StringValue("7")]
        AmountNotAllowed = 7,
        [StringValue("8")]
        AmountTooHigh = 8,
        [StringValue("9")]
        InvalidMessageVersion = 9,
        [StringValue("A")]
        TransactionStillBusy = 0xA

    }

    public struct TransactionResponse
    {
        public MessageType MessageResponseType { get; set; }
        public int TerminalId { get; set; }
        public AdditionalInformationType AdditionalInformationType { get; set; }
        public decimal Amount { get; set; }
        public string TypeOfCard { get; set; }
        public TransactionResult Result { get; set; }
        public const int MinLength = 27;
        //NOT PART OF PAYLOAD
        public string ErrorMessage { get; set; }

        public void FromString(string payload)
        {
            int index = 0;
            int length = 4;
            if (payload.Length >= (index + length))
            {
                MessageType messageType;
                MessageType.TryParse(payload.Substring(index, length), true, out messageType);
                MessageResponseType = messageType;
            }

            index += length;
            length = 8;
            if (payload.Length >= (index + length))
            {
                int terminalId;
                int.TryParse(payload.Substring(index, 8), out terminalId);
                TerminalId = terminalId;
            }

            index += length;
            length = 1;
            if (payload.Length >= (index + length))
            {
                AdditionalInformationType addInfo;
                AdditionalInformationType.TryParse(payload.Substring(index, length), true, out addInfo);
                AdditionalInformationType = addInfo;
            }

            index += length;
            length = 12;
            if (payload.Length >= (index + length))
            {
                decimal a;
                decimal.TryParse(payload.Substring(index, length), out a);
                Amount = a / 100;
            }

            index += length;
            length = 2;
            if (payload.Length >= (index + length))
            {
                TypeOfCard = payload.Substring(index, length);
            }

            index += length;
            length = 1;
            if (payload.Length >= (index + length))
            {
                TransactionResult rs;
                TransactionResult.TryParse(payload.Substring(index, length), true, out rs);
                Result = rs;
            }

        }
    }


    internal static class TerminalRequest
    {
        public static char CalculateLongitudinalRedundancyCheck(string source)
        {
            int result = 0;      
            for (int i = 0; i < source.Length; i++)
            {
                result = result ^ (Byte)(Encoding.ASCII.GetBytes(source.Substring(i, 1))[0]);
            }
            return (Char)result;
        }  
    }

    internal struct TransactionRequest
    {
        public MessageType MessageType { get; set; }
        public int TerminalId { get; set; }
        public bool HasAdditionalInformation 
        {
            get { return !String.IsNullOrEmpty(AdditionalInformation); }
        }
        public double Amount { get; set; }        
        public int ReceiptNumber { get; set; }
        public TransactionType TransactionType { get; set; }                   
        public AdditionalInformationType AdditionalInformationType { get; set; }
        public string AdditionalInformation { get; set; }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            // Message type (= Payment request), Length: 4, Fieldtype: AN
            result.Append(StringValueAttribute.GetStringValue(MessageType));
            // Terminal number, Length: 8, Fieldtype: N
            result.Append(TerminalId.ToString(CultureInfo.InvariantCulture).PadLeft(8, '0'));
            // Message version number, Length: 1, Fieldtype: N 
            // 0 = No addittional information, 1 = additional information
            result.Append(HasAdditionalInformation ? "1" : "0");                                    
            // Amount, Length: 11, Fieldtype: N
            result.Append(Amount.ToString("#.##", CultureInfo.CurrentCulture).Replace(
                CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "").PadLeft(11, '0'));
            // Product info, Length: 20, Fieldtype: AN (Future use)
            result.Append(new String(' ', 20));
            // Receipt number, Length: 6, Fieldtype: N
            result.Append(ReceiptNumber.ToString(CultureInfo.InvariantCulture).PadLeft(6, '0'));
            // Transaction type, Length: 2, Fieldtype: N
            // 00 = Purchase
            result.Append(StringValueAttribute.GetStringValue(TransactionType));
            if (HasAdditionalInformation)
            {
                // Length of additional information, Length: 3, Fieldtype: N
                var length = StringValueAttribute.GetStringValue(AdditionalInformationType).Length + AdditionalInformation.Length;
                Console.WriteLine("Length:" + length);
                result.Append(length.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0'));
               // result.Append(AdditionalInformation.Length .ToString(CultureInfo.InvariantCulture).PadLeft(3, '0'));
                // Indicator additional information, Length: 2, Fieldtype: N
                // 01 = Currency, 02 = Product information (only for petrol)
                // 03 = Product information (Special terminal)
                result.Append(StringValueAttribute.GetStringValue(AdditionalInformationType));
                // Additional information, Length: variable, Fieldtype: AN
                result.Append(AdditionalInformation);
            }
            // End of text
            result.Append((Char)TwoStepProtocol.EndOfText);
            // LRC Checksum
            result.Append(TerminalRequest.CalculateLongitudinalRedundancyCheck(result.ToString()));
            // Start of text
            result.Insert(0, (Char)TwoStepProtocol.StartOfText);
            return result.ToString();                                                                                                       
        }   
    }

    public class VerifoneOmni3750 : IDisposable
    {
        #region Instance fields

        private SerialPort serialPort = new SerialPort();        

        #endregion

        #region Constructor(s)

        public VerifoneOmni3750()
        {            
            Timeout = 10000;
            Retries = 3;
        }

        #endregion

        #region Properties
        
        public SerialPort SerialPort 
        { 
            get { return serialPort; }
        }

        public int TerminalId { get; set; }

        public int Timeout { get; set; }

        public int Retries { get; set; }

        #endregion

        #region Methods      

        private TwoStepProtocol WaitForResponse()
        {
            int timeout = Timeout;
            TwoStepProtocol result = TwoStepProtocol.Timeout;
            char response = (Char)TwoStepProtocol.Timeout;
            do
            {
                Console.WriteLine("WaitForResponse: Reading..");
                if (SerialPort.BufferCount() > 0)
                {
                    int r = SerialPort.ReadChar();
                       // (Char)(SerialPort.ReadChar().ToString(CultureInfo.InvariantCulture)[0]);
                    timeout = 0;
                    Console.WriteLine("Received response: {0}", r);
                    response = (Char)r.ToString(CultureInfo.InvariantCulture)[0];
                    TransactionResult trs;
                    TransactionResult.TryParse(response.ToString(), true, out trs);
                    Console.WriteLine("WaitForResponse: response:" + trs.ToString());
                }
                else
                {
                    Thread.Sleep(100);
                    timeout -= 100;
                }
            } while (timeout > 0);

            if (response == (Char)TwoStepProtocol.Acknowledge)
            {
                Console.WriteLine("WaitForResponse: response:TwoStepProtocol.Acknowledge");
                result = TwoStepProtocol.Acknowledge;
            }
            else
            {
                if (response == (Char)TwoStepProtocol.NegativeAcknowledge)
                {
                    Console.WriteLine("WaitForResponse: response:TwoStepProtocol.NegativeAcknowledge");
                    result = TwoStepProtocol.NegativeAcknowledge;
                }
            }

            return result;
        }
        public bool ReceiveTransactionResponse(out TransactionResponse transactionResponse)
        {
            Console.WriteLine("ReceiveTransactionResponse");
            int timeout = Timeout;
            String response = "";
            String error = "";
            do
            {
                Console.WriteLine("ReceiveTransactionResponse, bytes to Read:" + SerialPort.BytesToRead);
                if (SerialPort.BytesToRead > 0)
                {
                    SerialPort.ReadTimeout = 100;
                    response = SerialPort.ReadExisting();
                    timeout = 0;
                    Console.WriteLine("ReceiveTransactionResponse, response:" + response);
                }
                else
                {
                    Thread.Sleep(100);
                    timeout -= 100;
                }
            } while (timeout > 0);

            Console.WriteLine("Received response " +  response);

            transactionResponse = new TransactionResponse { ErrorMessage = error };
            if (response.Length >= TransactionResponse.MinLength)
            {
                transactionResponse.FromString(response);
                return true;
            }
            return false;
        }
        private bool SendRequest(string request)
        {
            Console.WriteLine("sending Request:" + request);
            SerialPort.WriteBinary(request);
            int retries = Retries;
            bool result = (WaitForResponse() == TwoStepProtocol.Acknowledge);
            while (!result && (retries > 0))
            {
                Console.WriteLine("Send request failed. Retrying...");
                retries -= 1;
                SerialPort.WriteBinary(request);
                result = (WaitForResponse() == TwoStepProtocol.Acknowledge);
            }
            return result;
        }

        public bool SendTransactionRequest(double amount, int receiptNumber, string currency)
        {
            TransactionRequest request = new TransactionRequest
            {
                MessageType = MessageType.TransactionRequest,
                TerminalId = this.TerminalId,                
                Amount = amount,
                ReceiptNumber = receiptNumber,
                TransactionType = TransactionType.Purchase,
                AdditionalInformationType = AdditionalInformationType.SpecialProductInformation, //Currency,
                AdditionalInformation = "Candy"
            };

            return SendRequest(request.ToString());          
        }      

        #endregion

        #region IDisposable Members

        private bool disposed;

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // Clean up any managed resources here.
                    // ...
                }
                // Clean up any unmanaged resources here.
                serialPort.Dispose();
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        ~VerifoneOmni3750()
        {
            Dispose(false);
        }        

        #endregion
    }            
}
