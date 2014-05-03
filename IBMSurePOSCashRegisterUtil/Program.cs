using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using Samba.Services;
using Timer = System.Timers.Timer;

namespace IBMSurePOSCashRegisterUtil
{
    internal class Program
    {
        const string PortName = "COM4";
        private static void Main(string[] args)
        {

            while (true)
            {
                Console.WriteLine("Enter your command:");
                String command = Console.ReadLine();
                if (!String.IsNullOrEmpty(command))
                {
                    String[] commands = command.Split(new char[] { ' ' });
                    foreach (var cmd in commands)
                    {
                        Console.WriteLine("Sending command :" + cmd);
                        SerialPortService.WriteHex(PortName, cmd);
                    }
                }

               
                ReadCashRegisterStatus(PortName, null);
            }
            /*
            byte b = (byte)int.Parse(args[0]);
            Console.WriteLine("Received by:" + (int)b);
            if (CheckBitOn(b, 3))
                Console.WriteLine("Unsolicited on");
            else Console.WriteLine("unsolicited off");
                
        

            if (CheckBitOn(b, 5) )
            {
                if(CheckBitOn(b, 7))
                   Console.WriteLine("Cash Drawer 1 is opened");
                else Console.WriteLine("Cash Drawer 1 is closed");
            }else if (CheckBitOn(b, 4) ) 
            {
                if (CheckBitOn(b, 6))
                    Console.WriteLine("Cash Drawer 2 is opened");
                else Console.WriteLine("Cash Drawer 2 is closed");
            }
            return;
             */
            
            if (args.Length > 0)
            {
                if (args[0] == "STATUS")
                {


                    if (ReadCashRegisterStatus(PortName, "1B06"))
                    {
                        Console.WriteLine("Cash register is open.");
                    }
                    else
                    {
                        Console.WriteLine("Cash register is closed.");
                    }

                }
                else if (args[0] == "OPEN")
                {
                    Console.WriteLine("Sending command 07");
                    SerialPortService.WriteHex(PortName, "07");
                    Thread.Sleep(3000);
                    if (ReadCashRegisterStatus(PortName, "1B06"))
                    {
                        Console.WriteLine("Cash register is open.");
                    }
                    else
                    {
                        Console.WriteLine("Cash register is closed.");
                    }
                }else if (args.Length == 2)
                {
                    SerialPortService.WriteHex(PortName, args[0]);
                    if (ReadCashRegisterStatus(PortName, args[1]))
                    {
                        Console.WriteLine("Cash register is open.");
                    }
                    else
                    {
                        Console.WriteLine("Cash register is closed.");
                    }
                }
                else if (args.Length == 1)
                {
                    // SerialPortService.WriteHex(portName, args[0]);
                    if (ReadCashRegisterStatus(PortName, args[0]))
                    {
                        Console.WriteLine("Cash register is open.");
                    }
                    else
                    {
                        Console.WriteLine("Cash register is closed.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command. Usage: OPEN/STATUS");
                }
            }
            else
            {
               
                Console.WriteLine("Usage: OPEN/STATUS  or hex data");
                
            }
        }

        public static bool CheckBitOn(byte b, int bitNumber)
        {
            return ((b & (1 << bitNumber - 1)) != 0);
        }

        private static bool ReadCashRegisterStatus(string portName, string command)
        {

            if (!String.IsNullOrEmpty(command))
            {
                Console.WriteLine("Sending commnad:" + command);
                SerialPortService.WriteHex(portName, command);
            }
            int count = 0;
           // while (true)
          //  {
          //      count++;
                Console.WriteLine("Reading.. ");
                string error = "";
                string data = SerialPortService.ReadExisting(portName, -1, 5000, ref error);
                if (data != null)
                {
                    Console.WriteLine("Received bytes:" + data.Length);
                }
                Console.WriteLine("Received data:" + data);
                Console.WriteLine("Received Error:" + error);

                if (!String.IsNullOrEmpty(data))
                {
                    bool cashDrawerOpen = false;
                    var buffer = Encoding.ASCII.GetBytes(data);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        Console.WriteLine("Received byte:" + (int)buffer[0]);
                    }

                    if (CheckBitOn(buffer[0], 3))
                        Console.WriteLine("Unsolicited on");
                    else Console.WriteLine("unsolicited off");
                    if (CheckBitOn(buffer[0], 5))
                    {
                        if (CheckBitOn(buffer[0], 7))
                        {
                            Console.WriteLine("Cash Drawer 1 is opened");
                            return true;
                        }
                        else Console.WriteLine("Cash Drawer 1 is closed");
                    }
                    else if (CheckBitOn(buffer[0], 4))
                    {
                        if (CheckBitOn(buffer[0], 6))
                        {
                            Console.WriteLine("Cash Drawer 2 is opened");
                            return true;
                        }
                        else Console.WriteLine("Cash Drawer 2 is closed");
                    }
                    return false;
                    /*
                    //IBM specific bit 4 cash drawe 1, bit 3 cash drawe 2
                    if ((buffer[0] & (1 << 3 - 1)) != 0) //cash drawer 1 connected status bit 4
                    {
                        if ((buffer[0] & (1 << 5 - 1)) != 0) //bit 6 for status of cash drawe 1
                        {
                            cashDrawerOpen = true;
                        }
                    }else if ((buffer[0] & (1 << 2 - 1)) != 0) //cash drawer 2 connected status bit 3
                    {
                        if ((buffer[6] & (1 << 1 - 1)) != 0) ////bit 6 for status of cash drawe 1
                        {
                            cashDrawerOpen = true;
                        }
                    }
                    if (cashDrawerOpen)
                    {
                        return true;
                    }
                    return false;
                    */
                }
            ////    if (count > 5)
            //    {
             //       break;
            //    }
             //  Thread.Sleep(1000);
          //  }
            return false;

        }
       
    }


}
