﻿using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Samba.Infrastructure.Printing
{
    public enum LineAlignment
    {
        Left,
        Center,
        Right,
        Justify
    }

    internal class BitmapData
    {
        public BitArray Dots
        {
            get;
            set;
        }

        public int Height
        {
            get;
            set;
        }

        public int Width
        {
            get;
            set;
        }
    }

    public class LinePrinter
    {
        private readonly string _printerName;
        private IntPtr _hprinter = IntPtr.Zero;
        private readonly int _maxChars;
        private readonly int _codePage;

        public LinePrinter(string printerName, int maxChars, int codepage)
        {
            _maxChars = maxChars;
            _codePage = codepage;
            _printerName = printerName;
        }

        public void Beep(char times = '\x2', char duration = '\x5')
        {
            WriteData((char)0x1B + "B" + times + duration);
        }

        public void EnableBold()
        {
            WriteData((char)0x1B + "G" + (char)1);
        }

        public void DisableBold()
        {
            WriteData((char)0x1B + "G" + (char)0);
        }

        public void SelectTurkishCodePage()
        {
            WriteData((char)0x1B + (char)0x1D + "t" + (char)12);
        }

        public void Cut()
        {
            WriteData((char)0x1B + "d" + (char)1);
            WriteData((char)0x1D + "V" + (char)66 + (char)0);
        }

        public void WriteLine(string line)
        {
            WriteLine(line, 0, 0);
        }

        public void WriteLine(string line, int height, int width)
        {
            int h = height + (width * 16);
            WriteData((char)0x1D + "!" + (char)h);

            //if (alignment != LineAlignment.Justify)
            //    WriteData((char)0x1B + "a" + (char)((int)alignment));
            //else
            //{
            //    WriteData((char)0x1B + "a" + (char)0);
            //    line = PrinterHelper.AlignLine(_maxChars, width, line, alignment, true);
            //}

            WriteData((char)0x1B + "a" + (char)0);
            WriteData(line + (char)0xA);
        }

        public void PrintWindow(string line)
        {
            //var chars = "▒▓";
            const string tl = "┌";
            const string tr = "┐";
            const string bl = "└";
            const string br = "┘";
            const string vl = "│";
            const string hl = "─";
            const string s = "░";

            WriteLine(tl + hl.PadLeft(_maxChars - 2, hl[0]) + tr, 1, 0);
            string text = vl + line.PadLeft((((_maxChars - 2) + line.Length) / 2), s[0]);
            WriteLine(text + vl.PadLeft(_maxChars - text.Length, s[0]), 1, 0);
            if (_maxChars - 2 > 0)
            {
                WriteLine(bl + hl.PadLeft(_maxChars - 2, hl[0]) + br);
            }
            else
            {
                WriteLine(bl  + br);
            }
        }

        public void PrintFullLine(char lineChar)
        {
            WriteLine(lineChar.ToString().PadLeft(_maxChars, lineChar));
        }

        public void PrintCenteredLabel(string label, bool expandLabel)
        {
            if (expandLabel) label = ExpandLabel(label);
            string text = label.PadLeft((((_maxChars) + label.Length) / 2), '░');
            if ((_maxChars - text.Length) > 0)
            {
                WriteLine(text + "░".PadLeft(_maxChars - text.Length, '░'), 1, 0);
            }
            else
            {
                WriteLine(text, 1, 0);
            }
        }

        private static string ExpandLabel(string label)
        {
            string result = "";
            for (int i = 0; i < label.Length - 1; i++)
            {
                result += label[i] + " ";
            }
            result += label[label.Length - 1];
            return result;
        }

        public void StartDocument()
        {
            if (_hprinter == IntPtr.Zero)
                _hprinter = PrinterHelper.GetPrinter(_printerName);
        }

        public void WriteData(byte[] data)
        {
            if (_hprinter != IntPtr.Zero)
            {
                int dwWritten;
                if (!PrinterHelper.WritePrinter(_hprinter, data, data.Length, out dwWritten)) BombWin32();
            }
        }

        public void WriteData(string data)
        {
            byte[] pBytes = Encoding.GetEncoding(_codePage).GetBytes(data);
            WriteData(pBytes);
        }

        public void EndDocument()
        {
            PrinterHelper.EndPrinter(_hprinter);
            _hprinter = IntPtr.Zero;
        }

        private static void BombWin32()
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        public void PrintBitmap(string fileName)
        {
            if (File.Exists(fileName))
            {
                byte[] data = GetDocument(fileName);
                WriteData(data);
            }
        }

        private static BitmapData GetBitmapData(string bmpFileName)
        {
            using (var bitmap = (Bitmap)Image.FromFile(bmpFileName))
            {
                const int threshold = 127;
                var index = 0;
                var dimensions = bitmap.Width * bitmap.Height;
                var dots = new BitArray(dimensions);

                for (var y = 0; y < bitmap.Height; y++)
                {
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        var color = bitmap.GetPixel(x, y);
                        var luminance = (int)(color.R * 0.3 + color.G * 0.59 + color.B * 0.11);
                        dots[index] = (luminance < threshold);
                        index++;
                    }
                }

                return new BitmapData
                           {
                               Dots = dots,
                               Height = bitmap.Height,
                               Width = bitmap.Width
                           };
            }
        }

        private static void RenderLogo(BinaryWriter bw, string fileName)
        {
            var data = GetBitmapData(fileName);
            var dots = data.Dots;
            var width = BitConverter.GetBytes(data.Width);

            bw.Write(AsciiControlChars.Escape);
            bw.Write('3');
            bw.Write((byte)24);

            int offset = 0;

            while (offset < data.Height)
            {
                bw.Write(AsciiControlChars.Escape);
                bw.Write('*');         // bit-image mode
                bw.Write((byte)33);    // 24-dot double-density
                bw.Write(width[0]);  // width low byte
                bw.Write(width[1]);  // width high byte

                for (int x = 0; x < data.Width; ++x)
                {
                    for (int k = 0; k < 3; ++k)
                    {
                        byte slice = 0;

                        for (int b = 0; b < 8; ++b)
                        {
                            int y = (((offset / 8) + k) * 8) + b;

                            int i = (y * data.Width) + x;

                            bool v = false;
                            if (i < dots.Length)
                            {
                                v = dots[i];
                            }

                            slice |= (byte)((v ? 1 : 0) << (7 - b));
                        }

                        bw.Write(slice);
                    }
                }

                offset += 24;
                bw.Write(AsciiControlChars.Newline);
            }

            bw.Write(AsciiControlChars.Escape);
            bw.Write('3');
            bw.Write((byte)30);
        }

        private static byte[] GetDocument(string fileName)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(AsciiControlChars.Escape);
                bw.Write('@');

                RenderLogo(bw, fileName);

                bw.Flush();

                return ms.ToArray();
            }
        }

        public void OpenCashDrawer()
        {
            // http://social.msdn.microsoft.com/forums/en-US/netfxbcl/thread/35575dd8-7593-4fe6-9b57-64ad6b5f7ae6/
            WriteData(((char)27 + (char)112 + (char)0 + (char)25 + (char)250).ToString());
        }

        public void ExecCommand(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                var data = command.Trim().Split(',').Select(x => Convert.ToInt32(x)).Aggregate("", (current, i) => current + (char)i);
                WriteData(data);
            }
        }
    }
}


