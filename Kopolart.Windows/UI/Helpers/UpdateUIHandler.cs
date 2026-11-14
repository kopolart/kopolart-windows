using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Forms;
using System;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    public static class UpdateUIHandler
    {
        public static void PromptAndDownloadUpdate(
            Form parentForm,
            UpdateInfo info,
            bool isDark,
            IDownloadService downloadService,
            IUpdatePackageVerifier packageVerifier)
        {
            if (parentForm == null || parentForm.IsDisposed) return;
            ArgumentNullException.ThrowIfNull(downloadService);
            ArgumentNullException.ThrowIfNull(packageVerifier);

            using (var downloadDialog = new UpdateDownloadDialog(info.Url, info.Version, isDark, downloadService, packageVerifier))
            {
                downloadDialog.ShowDialog(parentForm);
            }
        }
    }
}
