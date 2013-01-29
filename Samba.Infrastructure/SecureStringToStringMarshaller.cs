/// Copyright (C) 2010 Douglas Day
/// All rights reserved.
/// MIT-licensed: http://www.opensource.org/licenses/mit-license.php

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Samba.Infrastructure
{
    public class SecureStringToStringMarshaler : IDisposable
    {

        private SecureString _secureString;
        private GCHandle _gch;

        public SecureString SecureString
        {
            get { return _secureString; }
            set
            {
                _secureString = value;
                UpdateStringValue();
            }
        }

        public string String { get; protected set; }

        public SecureStringToStringMarshaler()
        {
        }

        public SecureStringToStringMarshaler(SecureString ss)
        {
            SecureString = ss;
        }

        void UpdateStringValue()
        {
            Deallocate();

            unsafe
            {
                if (SecureString != null)
                {
                    int length = SecureString.Length;
                    String = new string('\0', length);

                    _gch = new GCHandle();

                    // Create a CER (Contrained Execution Region)
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { }
                    finally
                    {
                        // Pin our string, disallowing the garbage collector from
                        // moving it around.
                        _gch = GCHandle.Alloc(String, GCHandleType.Pinned);
                    }

                    IntPtr stringPtr = IntPtr.Zero;
                    RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(
                        delegate
                        {
                            // Create a CER (Contrained Execution Region)
                            RuntimeHelpers.PrepareConstrainedRegions();
                            try { }
                            finally
                            {
                                stringPtr = Marshal.SecureStringToBSTR(SecureString);
                            }

                            // Copy the SecureString content to our pinned string
                            char* pString = (char*)stringPtr;
                            char* pInsecureString = (char*)_gch.AddrOfPinnedObject();
                            for (int index = 0; index < length; index++)
                            {
                                pInsecureString[index] = pString[index];
                            }
                        },
                        delegate
                        {
                            if (stringPtr != IntPtr.Zero)
                            {
                                // Free the SecureString BSTR that was generated
                                Marshal.ZeroFreeBSTR(stringPtr);
                            }
                        },
                        null);
                }
            }
        }

        void Deallocate()
        {
            if (_gch.IsAllocated)
            {
                unsafe
                {
                    // Determine the length of the string
                    int length = String.Length;

                    // Zero each character of the string.
                    char* pInsecureString = (char*)_gch.AddrOfPinnedObject();
                    for (int index = 0; index < length; index++)
                    {
                        pInsecureString[index] = '\0';
                    }

                    // Free the handle so the garbage collector
                    // can dispose of it properly.
                    _gch.Free();
                }
            }
        }

        public void Dispose()
        {
            Deallocate();
        }

    }
}
