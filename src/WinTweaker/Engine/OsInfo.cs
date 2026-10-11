using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using WinTweaker.Models;

namespace WinTweaker.Engine
{
    public sealed class OsInfo
    {
        public OsFamily Family { get; private set; }
        public int Build { get; private set; }
        public string ProductName { get; private set; }
        public string Caption { get; private set; }

        public static OsInfo Detect()
        {
            var info = new OsVersionInfo();
            info.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OsVersionInfo));
            RtlGetVersion(ref info);

            var os = new OsInfo();
            os.Build = info.dwBuildNumber;
            os.ProductName = ReadProductName();

            if (info.dwMajorVersion == 6 && info.dwMinorVersion == 1)
                os.Family = OsFamily.Windows7;
            else if (info.dwMajorVersion == 10 && info.dwBuildNumber >= 22000)
                os.Family = OsFamily.Windows11;
            else if (info.dwMajorVersion == 10)
                os.Family = OsFamily.Windows10;
            else
                os.Family = OsFamily.Other;

            os.Caption = os.ProductName + " · build " + os.Build;
            return os;
        }

        public bool Supports(OsTarget target)
        {
            switch (Family)
            {
                case OsFamily.Windows7: return (target & OsTarget.Win7) != 0;
                case OsFamily.Windows10: return (target & OsTarget.Win10) != 0;
                case OsFamily.Windows11: return (target & OsTarget.Win11) != 0;
                default: return false;
            }
        }

        static string ReadProductName()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key == null)
                        return "Windows";
                    var name = key.GetValue("ProductName") as string;
                    return string.IsNullOrWhiteSpace(name) ? "Windows" : name;
                }
            }
            catch
            {
                return "Windows";
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct OsVersionInfo
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        [DllImport("ntdll.dll")]
        static extern int RtlGetVersion(ref OsVersionInfo info);
    }
}
