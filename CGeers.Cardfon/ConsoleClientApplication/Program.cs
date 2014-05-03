using System;
using System.IO.Ports;
using CGeers.Cardfon;

namespace ConsoleClientApplication
{
    class Program
    {
        static void Main(string[] args)
        {            
            using (var omniTerminal = new VerifoneOmni3750 { TerminalId = 5445685 })
            {
                int port = 3;
                if (args.Length > 0)
                {
                    port = int.Parse(args[0]);
                }
                // Set the communication settings of the serial port.
                omniTerminal.SerialPort.BaudRate = 9600;
                omniTerminal.SerialPort.Parity = Parity.Even;
                omniTerminal.SerialPort.DataBits = 7;
                omniTerminal.SerialPort.StopBits = StopBits.One;

                // Establish a connection to the serial port (COM1).
                omniTerminal.SerialPort.Open(port);                                
                omniTerminal.SendTransactionRequest(10.1, 222, "USD");
                TransactionResponse transactionResponse;
                omniTerminal.ReceiveTransactionResponse(out transactionResponse);
                Console.WriteLine("Amount:" + transactionResponse.Amount);
                Console.WriteLine("Error:" + transactionResponse.ErrorMessage);
                Console.WriteLine("Result:" + transactionResponse.Result);
                Console.WriteLine("TerminalId:" + transactionResponse.TerminalId);
                Console.WriteLine("TypeOfCard:" + transactionResponse.TypeOfCard);
                
            }            

            Console.ReadLine();
        }
    }
}
