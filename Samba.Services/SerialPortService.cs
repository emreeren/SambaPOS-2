using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace Samba.Services
{
    public static class SerialPortService
    {

        public  delegate void SerialDataReceivedEventHanderDelegate(object sender, SerialDataReceivedEventArgs e);

        private static readonly Dictionary<string, SerialPort> Ports = new Dictionary<string, SerialPort>();

        public static void AddDataReceivedEventDelegate(string portName, int baudRate,
                                           SerialDataReceivedEventHanderDelegate dataReceivedEventHanderDelegate)
        {
            if (!Ports.ContainsKey(portName))
            {
                if (baudRate > 0)
                {
                    Ports.Add(portName, new SerialPort(portName, baudRate));
                }
                else
                {
                    Ports.Add(portName, new SerialPort(portName));
                }
            }
            var port = Ports[portName];
             port.DataReceived += new SerialDataReceivedEventHandler(dataReceivedEventHanderDelegate);

            try
            {
                if (port.BaudRate != baudRate)
                {
                    port.Close();
                }
                if (!port.IsOpen) port.Open();

            }
            catch (Exception ex)
            {
                
            }


        }

        public static void RemoveDataReceivedEventDelegate(string portName,
                                           SerialDataReceivedEventHanderDelegate dataReceivedEventHanderDelegate)
        {
            if (Ports.ContainsKey(portName))
            {

                var port = Ports[portName];
                port.DataReceived -= new SerialDataReceivedEventHandler(dataReceivedEventHanderDelegate);
            }

            

        }
        

        public static void WritePort(string portName, byte[] data)
        {
            if (!Ports.ContainsKey(portName))
            {
                Ports.Add(portName, new SerialPort(portName));
            }
            var port = Ports[portName];

            try
            {
                if (!port.IsOpen) port.Open();
                if (port.IsOpen) port.Write(data, 0, data.Length);
            }
            catch (IOException)
            {
                port.Close();
            }
            catch (Exception)
            {
                port.Close();
            }
        }

        public static SerialPort GetPort(string portName)
        {
            if (!Ports.ContainsKey(portName))
            {
                Ports.Add(portName, new SerialPort(portName));
            }
            return Ports[portName];
        }

        public static void WritePort(string portName, string data)
        {
            WritePort(portName, Encoding.ASCII.GetBytes(data));
        }

        public static void WritePort(string portName, string data, int codePage)
        {
            WritePort(portName, Encoding.GetEncoding(codePage).GetBytes(data));
        }

        public static byte[] ReadBufferPort(string portName, int length, int timeoutInMillSec)
        {
            if (!Ports.ContainsKey(portName))
            {
                Ports.Add(portName, new SerialPort(portName));
            }
            var port = Ports[portName];

            try
            {
                if (!port.IsOpen) port.Open();
                if (port.IsOpen)
                {
                    if (timeoutInMillSec >= 0)
                    {
                        port.ReadTimeout = timeoutInMillSec;
                    }

                    var buffer = new byte[length];
                    port.Read(buffer, 0, length);
                    return buffer;
                }
            }
            catch (IOException)
            {
                port.Close();
            }
            catch (Exception)
            {
                port.Close();
            }
            return null;
        }

        public static string ReadPort(string portName, int length, int timeoutInMillSec = -1)
        {
            var buffer = ReadBufferPort(portName, length, timeoutInMillSec);
            return Encoding.ASCII.GetString(buffer);
        }

        public static string ReadExisting(string portName, int baudRate, int timeoutInMillSec, ref string error)
        {
            if (!Ports.ContainsKey(portName))
            {
                if (baudRate > 0)
                {
                    Ports.Add(portName, new SerialPort(portName, baudRate));
                }
                else
                {
                    Ports.Add(portName, new SerialPort(portName));
                }
            }
            var port = Ports[portName];

            try
            {
                if (port.BaudRate != baudRate)
                {
                    port.Close();
                }
                if (!port.IsOpen) port.Open();
                if (port.IsOpen)
                {
                    if (timeoutInMillSec >= 0)
                    {
                        port.ReadTimeout = timeoutInMillSec;
                    }


                    while (timeoutInMillSec > 0)
                    {
                        
                        string data = port.ReadExisting();
                        if (!String.IsNullOrEmpty(data))
                        {
                            return data;
                        }
                        Thread.Sleep(1000);
                        timeoutInMillSec -= 1000;
                    }


                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }
            return "";
        }

        public static void ResetCache()
        {
            foreach (var key in Ports.Keys)
                Ports[key].Close();
            Ports.Clear();
        }

        internal static void WriteCommand(string portName, string command, int codePage)
        {
            if (!string.IsNullOrEmpty(command))
            {
                var data = command.Trim().Split(',').Select(x => Convert.ToInt32(x)).Aggregate("", (current, i) => current + (char)i);
                WritePort(portName, data, codePage);
            }
        }
    }
}
