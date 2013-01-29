using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;


namespace Samba.Presentation.Common.VirtualKeyboard
{
    public enum KeyState
    {
        FirstSet,
        SecondSet
    }

    public class VKey : ObservableObject
    {
        private KeyState _keyState;
        public KeyState KeyState { get { return _keyState; } set { _keyState = value; RaisePropertyChanged("Caption"); } }

        public string Caption
        {
            get { return KeyState == KeyState.FirstSet ? LowKey : UpKey; }
        }

        public string LowKey { get; set; }
        public string UpKey { get; set; }
        public Keys VirtualKey { get; set; }

        public VKey(string lowKey, string upKey, Keys virtualKey)
        {
            LowKey = lowKey;
            UpKey = upKey;
            VirtualKey = virtualKey;
        }

        public VKey(Keys virtualKey)
        {
            VirtualKey = virtualKey;

            try
            {
                LowKey = User32Interop.ToUnicode(virtualKey, Keys.None).ToString();
            }
            catch (Exception) { LowKey = " "; }

            try
            {
                UpKey = User32Interop.ToUnicode(virtualKey, Keys.ShiftKey).ToString();
            }
            catch (Exception) { UpKey = " "; }
        }
    }

    public static class User32Interop
    {
        public static char ToUnicode(Keys key, Keys modifiers)
        {
            var chars = new char[1];
            int result = ToUnicode((uint)key, 0, GetKeyState(modifiers),
                                 chars, chars.Length, 0);
            if (result == 1)
            {
                return chars[0];
            }

            result = ToUnicode((uint)key, 0, GetKeyState(modifiers), chars, chars.Length, 0);
            if (result == 1)
            {
                return chars[0];
            }

            throw new Exception("Invalid key");
        }

        private const byte HighBit = 0x80;
        private static byte[] GetKeyState(Keys modifiers)
        {
            var keyState = new byte[256];
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if ((modifiers & key) == key)
                {
                    keyState[(int)key] = HighBit;
                }
            }
            return keyState;
        }

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern int ToUnicode(uint virtualKey, uint scanCode, byte[] keyStates, [MarshalAs(UnmanagedType.LPArray)] [Out] char[] chars, int charMaxCount, uint flags);

    }
}
