using Kopolart.Windows.Application.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace Kopolart.Windows.Infrastructure.Security
{
    public sealed class UpdatePackageVerifier : IUpdatePackageVerifier
    {
        // Single-file bundle is normally > 15MB; anything under 1MB is corrupt or an error page
        private const long MinimumPackageSizeBytes = 1024 * 1024;

        public bool ValidatePackage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < MinimumPackageSizeBytes)
            {
                Debug.WriteLine($"Update validation failed: downloaded file size ({fileInfo.Length} bytes) is too small");
                return false;
            }

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                bool matchesCompanyOrProduct =
                    string.Equals(versionInfo.ProductName, "Kopolart", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(versionInfo.CompanyName, "Kopolart", StringComparison.OrdinalIgnoreCase);

                if (!matchesCompanyOrProduct)
                {
                    Debug.WriteLine($"Update validation failed: unexpected product/company ({versionInfo.ProductName} / {versionInfo.CompanyName})");
                    return false;
                }

                // Only demand a signature when the running binary itself is signed.
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(currentExe) && AuthenticodeVerifier.IsSigned(currentExe) &&
                    !AuthenticodeVerifier.HasValidAuthenticodeSignature(filePath))
                {
                    Debug.WriteLine($"Update validation failed: Authenticode signature verification failed for {filePath}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update validation encountered an error for {filePath}: {ex}");
                return false;
            }
        }
    }
}
