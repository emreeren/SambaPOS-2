using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Samba.Infrastructure.Printing
{
    public static class AsciiControlChars
    {
        /// <summary>
        /// Usually indicates the end of a string.
        /// </summary>
        public const char Nul = (char)0x00;

        /// <summary>
        /// Meant to be used for printers. When receiving this code the 
        /// printer moves to the next sheet of paper.
        /// </summary>
        public const char FormFeed = (char)0x0C;

        /// <summary>
        /// Starts an extended sequence of control codes.
        /// </summary>
        public const char Escape = (char)0x1B;

        /// <summary>
        /// Advances to the next line.
        /// </summary>
        public const char Newline = (char)0x0A;

        /// <summary>
        /// Defined to separate tables or different sets of data in a serial
        /// data storage system.
        /// </summary>
        public const char GroupSeparator = (char)0x1D;

        /// <summary>
        /// A horizontal tab.
        /// </summary>
        public const char HorizontalTab = (char)0x09;

        /// <summary>
        /// Returns the carriage to the start of the line.
        /// </summary>
        public const char CarriageReturn = (char)0x0D;

        /// <summary>
        /// Cancels the operation.
        /// </summary>
        public const char Cancel = (char)0x18;

        /// <summary>
        /// Indicates that control characters present in the stream should
        /// be passed through as transmitted and not interpreted as control
        /// characters.
        /// </summary>
        public const char DataLinkEscape = (char)0x10;

        /// <summary>
        /// Signals the end of a transmission.
        /// </summary>
        public const char EndOfTransmission = (char)0x04;

        /// <summary>
        /// In serial storage, signals the separation of two files.
        /// </summary>
        public const char FileSeparator = (char)0x1C;
    }

    public class PrinterHelper
    {
        private static string GetTag(string line)
        {
            if (Regex.IsMatch(line, "<[^>]+>"))
            {
                var tag = Regex.Match(line, "<[^>]+>").Groups[0].Value;
                return tag;
            }
            return "";
        }

        public static IEnumerable<string> ReplaceChars(IEnumerable<string> lines, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return lines;
            var result = new List<string>(lines);
            var patterns = pattern.Split(';');
            foreach (var s in patterns)
            {
                var parts = s.Split('=');
                for (var i = 0; i < result.Count; i++)
                {
                    result[i] = result[i].Replace(parts[0], parts[1]);
                }
            }
            return result;
        }

        public static IEnumerable<string> AlignLines(IEnumerable<string> lines, int maxWidth, bool canBreak)
        {
            var columnWidths = CalculateColumnWidths(lines);
            var result = new List<string>();

            for (var i = 0; i < lines.Count(); i++)
            {
                var line = lines.ElementAt(i);
                var lastWidth = 0;
                if (line.Length > 3 && Char.IsNumber(line[3]) && Char.IsNumber(line[2]))
                {
                    lastWidth = Convert.ToInt32(line[3].ToString());
                }
                if (line.Length < 4)
                {
                    result.Add(line);
                }
                else if (line.ToLower().StartsWith("<l"))
                {
                    result.Add(AlignLine(maxWidth, lastWidth, line, LineAlignment.Left, canBreak));
                }
                else if (line.ToLower().StartsWith("<r"))
                {
                    result.Add(AlignLine(maxWidth, lastWidth, line, LineAlignment.Right, canBreak));
                }
                else if (line.ToLower().StartsWith("<c"))
                {
                    result.Add(AlignLine(maxWidth, lastWidth, line, LineAlignment.Center, canBreak));
                }
                else if (line.ToLower().StartsWith("<j"))
                {
                    result.Add(AlignLine(maxWidth, lastWidth, line, LineAlignment.Justify, canBreak, columnWidths[0]));
                    if (i < lines.Count() - 1 && columnWidths.Count > 0 && (!lines.ElementAt(i + 1).ToLower().StartsWith("<j") || lines.ElementAt(i + 1).Split('|').Length != columnWidths[0].Length))
                        columnWidths.RemoveAt(0);
                }
                else if (line.ToLower().StartsWith("<f>"))
                {
                    var c = line.Contains(">") ? line.Substring(line.IndexOf(">") + 1).Trim() : line.Trim();
                    if (c.Length == 1)
                        result.Add(c.PadLeft(maxWidth, c[0]));
                }
                else result.Add(line);
            }

            return result;
        }

        private static IList<int[]> CalculateColumnWidths(IEnumerable<string> lines)
        {
            var result = new List<int[]>();
            var tableNo = 0;
            foreach (var line in lines)
            {
                if (line.ToLower().StartsWith("<j"))
                {
                    var parts = line.Split('|');
                    if (tableNo == 0 || parts.Length != result[tableNo - 1].Length)
                    {
                        tableNo = result.Count + 1;
                        result.Add(new int[parts.Length]);
                    }

                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (result[tableNo - 1][i] < parts[i].Length)
                            result[tableNo - 1][i] = parts[i].Length;
                    }
                }
                else
                {
                    tableNo = 0;
                }
            }
            return result;
        }

        public static string AlignLine(int maxWidth, int width, string line, LineAlignment alignment, bool canBreak, int[] columnWidths = null)
        {
            maxWidth = maxWidth / (width + 1);

            var tag = GetTag(line);
            line = line.Replace(tag, "");

            switch (alignment)
            {
                case LineAlignment.Left:
                    return tag + line.PadRight(maxWidth, ' ');
                case LineAlignment.Right:
                    return tag + line.PadLeft(maxWidth, ' ');
                case LineAlignment.Center:
                    return tag + line.PadLeft(((maxWidth + line.Length) / 2), ' ').PadRight(maxWidth, ' ');
                case LineAlignment.Justify:
                    return tag + JustifyText(maxWidth, line, canBreak, columnWidths);
                default:
                    return tag + line;
            }
        }

        private static string JustifyText(int maxWidth, string line, bool canBreak, IList<int> columnWidths = null)
        {
            var parts = line.Split('|');
            if (parts.Length == 1) return line;

            var text = "";
            for (var i = parts.Length - 1; i > 0; i--)
            {
                var l = columnWidths != null ? columnWidths[i] : parts[i].Length;
                parts[i] = parts[i].Trim().PadLeft(l);
                text = parts[i] + text;
            }

            if (!canBreak && parts[0].Length > maxWidth)
                parts[0] = parts[0].Substring(0, maxWidth);

            if (canBreak && parts[0].Length + text.Length > maxWidth)
            {
                return parts[0].Trim() + "\n" + text.PadLeft(maxWidth);
            }

            return parts[0].PadRight(maxWidth - text.Length).Substring(0, maxWidth - text.Length) + text;
        }

        public static IntPtr GetPrinter(string szPrinterName)
        {
            var di = new DOCINFOA { pDocName = "Samba POS Document", pDataType = "RAW" };
            IntPtr hPrinter;
            if (!OpenPrinter(szPrinterName, out hPrinter, IntPtr.Zero)) BombWin32();
            if (!StartDocPrinter(hPrinter, 1, di)) BombWin32();
            if (!StartPagePrinter(hPrinter)) BombWin32();
            return hPrinter;
        }

        public static void EndPrinter(IntPtr hPrinter)
        {
            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
            ClosePrinter(hPrinter);
        }

        public static void SendBytesToPrinter(string szPrinterName, byte[] pBytes)
        {
            var hPrinter = GetPrinter(szPrinterName);
            int dwWritten;
            if (!WritePrinter(hPrinter, pBytes, pBytes.Length, out dwWritten)) BombWin32();
            EndPrinter(hPrinter);
        }

        public static void SendFileToPrinter(string szPrinterName, string szFileName)
        {
            var fs = new FileStream(szFileName, FileMode.Open);
            var len = (int)fs.Length;
            var bytes = new Byte[len];
            fs.Read(bytes, 0, len);
            SendBytesToPrinter(szPrinterName, bytes);
        }

        private static void BombWin32()
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DOCINFOA
        {
            public string pDocName;
            public string pOutputFile;
            public string pDataType;
        }

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);
        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, DOCINFOA di);
        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, Int32 dwCount, out Int32 dwWritten);

    }
}


