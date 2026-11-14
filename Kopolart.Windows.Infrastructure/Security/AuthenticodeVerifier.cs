using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Kopolart.Windows.Infrastructure.Security
{
    /// <summary>
    /// Verifies Authenticode signatures via WinVerifyTrust so downloaded executables
    /// are never launched (elevated) without a valid, trusted signature.
    /// </summary>
    public static class AuthenticodeVerifier
    {
        private static readonly Guid WintrustActionGenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        private const uint WTD_UI_NONE = 2;
        private const uint WTD_REVOKE_NONE = 0;
        private const uint WTD_CHOICE_FILE = 1;
        private const uint WTD_STATEACTION_VERIFY = 1;
        private const uint WTD_STATEACTION_CLOSE = 2;
        private const uint WTD_SAFER_FLAG = 0x100;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        private static extern int WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
            ref WINTRUST_DATA pWVTData);

        /// <summary>
        /// Returns true when the file carries a valid Authenticode signature chaining to a
        /// trusted root AND the signer subject is Microsoft.
        /// </summary>
        public static bool IsTrustedMicrosoftBinary(string filePath)
        {
            try
            {
                if (!HasValidAuthenticodeSignature(filePath))
                {
                    return false;
                }

#pragma warning disable SYSLIB0057
                using var cert = X509Certificate2.CreateFromSignedFile(filePath);
#pragma warning restore SYSLIB0057
                return cert.Subject.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Authenticode verification failed for '{filePath}': {ex}");
                return false;
            }
        }

        /// <summary>
        /// Returns true if the file carries any readable X509 certificate.
        /// </summary>
        public static bool IsSigned(string filePath)
        {
            try
            {
#pragma warning disable SYSLIB0057
                using var cert = X509Certificate2.CreateFromSignedFile(filePath);
#pragma warning restore SYSLIB0057
                return cert != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks WinVerifyTrust to ensure the file carries a valid, trusted Authenticode signature.
        /// </summary>
        public static bool HasValidAuthenticodeSignature(string filePath)
        {
            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                pcwszFilePath = filePath,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };

            var pFile = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
            try
            {
                Marshal.StructureToPtr(fileInfo, pFile, false);

                var trustData = new WINTRUST_DATA
                {
                    cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                    dwUIChoice = WTD_UI_NONE,
                    fdwRevocationChecks = WTD_REVOKE_NONE,
                    dwUnionChoice = WTD_CHOICE_FILE,
                    pFile = pFile,
                    dwStateAction = WTD_STATEACTION_VERIFY,
                    dwProvFlags = WTD_SAFER_FLAG
                };

                int result = WinVerifyTrust(IntPtr.Zero, WintrustActionGenericVerifyV2, ref trustData);

                trustData.dwStateAction = WTD_STATEACTION_CLOSE;
                WinVerifyTrust(IntPtr.Zero, WintrustActionGenericVerifyV2, ref trustData);

                return result == 0;
            }
            finally
            {
                Marshal.DestroyStructure<WINTRUST_FILE_INFO>(pFile);
                Marshal.FreeHGlobal(pFile);
            }
        }
    }
}
