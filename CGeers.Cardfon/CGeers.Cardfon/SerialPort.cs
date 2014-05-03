using System;
using System.Text;
using System.IO.Ports;
using System.Globalization;

namespace CGeers.Cardfon
{
    public static class SerialPortExtensions
    {
        public static int BufferCount(this SerialPort port)
        {
            if (port.IsOpen)
            {
                return port.BytesToRead;
            }
            else
            {
                return 0;
            }
        }

        public static void Open(this SerialPort port, int portNumber)
        {
            port.PortName = "COM" + portNumber.ToString(CultureInfo.InvariantCulture);
            port.Open();
        }

        public static void WriteBinary(this SerialPort port, string command)
        {
            byte[] buffer = new byte[command.Length];
            for (int i = 0; i < command.Length; i++)
            {
                buffer[i] = (Byte)(Encoding.ASCII.GetBytes(command.Substring(i, 1))[0]);
            }
            port.Write(buffer, 0, buffer.Length);
        }                
    }
}
