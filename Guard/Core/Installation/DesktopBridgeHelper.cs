using System.Runtime.InteropServices;
using System.Text;

/**
 * The MIT License (MIT)
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * https://github.com/microsoft/Windows-AppConsult-Tools-DesktopBridgeHelpers/
 * Modified methods to be static
 */
namespace Guard.Core.Models
{
    internal class DesktopBridgeHelper
    {
        const long APPMODEL_ERROR_NO_PACKAGE = 15700L;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetCurrentPackageFullName(
            ref int packageFullNameLength,
            StringBuilder packageFullName
        );

        public static bool IsRunningAsUwp()
        {
            if (IsWindows7OrLower)
            {
                return false;
            }
            else
            {
                int length = 0;
                StringBuilder sb = new(0);
                int result = GetCurrentPackageFullName(ref length, sb);

                sb = new StringBuilder(length);
                result = GetCurrentPackageFullName(ref length, sb);

                return result != APPMODEL_ERROR_NO_PACKAGE;
            }
        }

        private static bool IsWindows7OrLower
        {
            get
            {
                int versionMajor = Environment.OSVersion.Version.Major;
                int versionMinor = Environment.OSVersion.Version.Minor;
                double version = versionMajor + (double)versionMinor / 10;
                return version <= 6.1;
            }
        }
    }
}
