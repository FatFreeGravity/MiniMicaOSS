using System;
using System.Runtime.InteropServices;

namespace MiniMicaApp.Platform.Windows
{
    /// <summary>
    /// OS baseline and per-feature capability checks.
    ///
    /// The app runs anywhere .NET Framework 4.8 is in the box, which is Windows 10 1903
    /// (build 18362) and later. Verified on Windows 10 20H1 (19041): no Mica, square
    /// corners, no Snap Layouts flyout, and Segoe Fluent Icons absent, but the app runs
    /// and every function works. Windows 10 is an edge case for OEMs in 2026, not a
    /// reason to refuse to start.
    ///
    /// Visual features degrade rather than block:
    ///   Mica / Acrylic / Tabbed   Windows 11 22H2 (22621)
    ///   rounded corners           Windows 11 21H2 (22000)
    ///   Snap Layouts flyout       Windows 11 only; the button still works as a button
    ///   Segoe Fluent Icons        Windows 11 only; icon fonts fall back to Segoe MDL2 Assets
    ///
    /// RtlGetVersion is used instead of Environment.OSVersion because it is not subject
    /// to compatibility-manifest version shimming.
    /// </summary>
    public static class WindowsVersion
    {
        /// <summary>Windows 10 1903. The first build shipping .NET Framework 4.8 in the box.</summary>
        public const int MinimumBuild = 18362;

        /// <summary>Windows 11 21H2. DWMWA_WINDOW_CORNER_PREFERENCE.</summary>
        public const int RoundedCornersBuild = 22000;

        /// <summary>Windows 11 22H2. DWMWA_SYSTEMBACKDROP_TYPE (Mica, Acrylic, Tabbed).</summary>
        public const int BackdropBuild = 22621;

        public static int Build
        {
            get { return GetVersion().Build; }
        }

        /// <summary>
        /// Can the app run at all. False only below the .NET Framework 4.8 in-box
        /// baseline, where the runtime itself may be absent.
        /// </summary>
        public static bool IsSupported
        {
            get
            {
                Version version = GetVersion();
                return version.Major >= 10 && version.Build >= MinimumBuild;
            }
        }

        /// <summary>Windows 11 21H2 or later: DWM can round the window corners.</summary>
        public static bool SupportsRoundedCorners
        {
            get { return GetVersion().Build >= RoundedCornersBuild; }
        }

        /// <summary>Windows 11 22H2 or later: DWM can supply a system backdrop.</summary>
        public static bool SupportsBackdrop
        {
            get { return GetVersion().Build >= BackdropBuild; }
        }

        public static string Description
        {
            get
            {
                Version version = GetVersion();
                if (version.Major >= 10 && version.Build >= RoundedCornersBuild)
                {
                    return "Windows 11 build " + version.Build;
                }
                if (version.Major >= 10)
                {
                    return "Windows 10 build " + version.Build;
                }
                return "Windows " + version.Major + "." + version.Minor + " build " + version.Build;
            }
        }

        private static Version GetVersion()
        {
            RTL_OSVERSIONINFO versionInfo = new RTL_OSVERSIONINFO();
            versionInfo.dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(RTL_OSVERSIONINFO));
            versionInfo.szCSDVersion = string.Empty;

            int status = RtlGetVersion(ref versionInfo);
            if (status == 0)
            {
                return new Version(
                    (int)versionInfo.dwMajorVersion,
                    (int)versionInfo.dwMinorVersion,
                    (int)versionInfo.dwBuildNumber);
            }

            return Environment.OSVersion.Version;
        }

        [DllImport("ntdll.dll", CharSet = CharSet.Unicode)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFO versionInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RTL_OSVERSIONINFO
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }
    }
}
