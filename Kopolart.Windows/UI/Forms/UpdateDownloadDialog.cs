using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.Application.Interfaces;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class UpdateDownloadDialog : ThemedDialog
    {
        private readonly string _downloadUrl;
        private readonly string _expectedVersion;
        private readonly IDownloadService _downloadService;
        private readonly IUpdatePackageVerifier _packageVerifier;
        private CancellationTokenSource? _cts;

        private Label lblStatus = null!;
        private ProgressBar progressBar = null!;
        private Button btnCancel = null!;
        private Button btnRetry = null!;

        public UpdateDownloadDialog(
            string downloadUrl,
            string expectedVersion,
            bool isDark,
            IDownloadService downloadService,
            IUpdatePackageVerifier packageVerifier)
            : base(isDark)
        {
            _downloadUrl = downloadUrl;
            _expectedVersion = expectedVersion ?? string.Empty;
            _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
            _packageVerifier = packageVerifier ?? throw new ArgumentNullException(nameof(packageVerifier));

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            InitializeTheme();
        }

        private void InitializeComponent()
        {
            lblStatus = new Label();
            progressBar = new ProgressBar();
            btnCancel = new Button();
            btnRetry = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(400, 140);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("UpdatingTitle");

            lblStatus.Location = new Point(20, 20);
            lblStatus.Size = new Size(360, 20);
            lblStatus.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            lblStatus.Text = TextManager.GetString("DownloadingStarting");

            progressBar.Location = new Point(20, 50);
            progressBar.Size = new Size(360, 23);

            btnCancel.Text = TextManager.GetString("Cancel");
            btnCancel.Size = new Size(90, 30);
            btnCancel.Location = new Point(190, 90);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnCancel.Click += BtnCancel_Click;

            btnRetry.Text = TextManager.GetString("LoaderRetry");
            btnRetry.Size = new Size(90, 30);
            btnRetry.Location = new Point(290, 90);
            btnRetry.Cursor = Cursors.Hand;
            btnRetry.FlatStyle = FlatStyle.Flat;
            btnRetry.FlatAppearance.BorderSize = 0;
            btnRetry.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnRetry.Visible = false;
            btnRetry.Click += BtnRetry_Click;

            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(btnCancel);
            Controls.Add(btnRetry);

            ResumeLayout(false);
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblStatus.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnCancel.BackColor = Color.FromArgb(60, 60, 60);
                btnCancel.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnRetry.BackColor = UIConstants.DarkMode.ThemeColor;
                btnRetry.ForeColor = Color.White;
                return;
            }

            BackColor = Color.White;
            lblStatus.ForeColor = Color.FromArgb(51, 51, 51);
            btnCancel.BackColor = Color.FromArgb(240, 240, 240);
            btnCancel.ForeColor = Color.FromArgb(85, 85, 85);
            btnRetry.BackColor = UIConstants.ThemeColor;
            btnRetry.ForeColor = Color.FromArgb(74, 59, 42);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _cts = new CancellationTokenSource();
            await StartDownloadAsync(_cts.Token);
        }

        private async Task StartDownloadAsync(CancellationToken token)
        {
            string? tempNewExe = null;
            try
            {
                var currentExe = Infrastructure.Helpers.AppPathHelper.GetTargetExecutablePath() ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExe))
                {
                    throw new Exception("Unable to determine executable path.");
                }

                // Store downloaded update in %TEMP% instead of the installation directory to prevent
                // UnauthorizedAccessException when installed under Program Files.
                tempNewExe = Path.Combine(Path.GetTempPath(), $"kopolart-update-{Guid.NewGuid():N}.exe");

                var progress = new Progress<DownloadProgressReport>(report =>
                {
                    if (IsDisposed) return;
                    if (report.TotalBytes.HasValue && report.TotalBytes.Value > 0)
                    {
                        var percentage = Math.Clamp(report.Percentage, 0, 100);
                        progressBar.Value = percentage;

                        string progressFormat = TextManager.GetString("DownloadingProgress");
                        lblStatus.Text = string.Format(progressFormat, 
                            percentage, 
                            (report.BytesReceived / 1024.0 / 1024.0).ToString("F1"), 
                            (report.TotalBytes.Value / 1024.0 / 1024.0).ToString("F1"));
                    }
                    else
                    {
                        string noSizeFormat = TextManager.GetString("DownloadingNoSize");
                        lblStatus.Text = string.Format(noSizeFormat, 
                            (report.BytesReceived / 1024.0 / 1024.0).ToString("F1"));
                    }
                });

                await _downloadService.DownloadFileAsync(_downloadUrl, tempNewExe, progress, token);

                lblStatus.Text = TextManager.GetString("DownloadingComplete");
                await Task.Delay(1000, token);

                // Never swap the executable if the user dismissed the dialog meanwhile.
                if (token.IsCancellationRequested || IsDisposed)
                {
                    TryDeleteFile(tempNewExe);
                    return;
                }

                // Validate downloaded file integrity and metadata before launching file swap
                if (!_packageVerifier.ValidatePackage(tempNewExe))
                {
                    TryDeleteFile(tempNewExe);
                    throw new InvalidOperationException("Downloaded update failed integrity verification.");
                }

                LaunchFileSwap(currentExe, tempNewExe);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                if (tempNewExe != null) TryDeleteFile(tempNewExe);
                if (!IsDisposed && Visible)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }
            catch (Exception ex)
            {
                if (tempNewExe != null) TryDeleteFile(tempNewExe);
                Debug.WriteLine($"Failed to download update from {_downloadUrl}: {ex}");
                if (!IsDisposed)
                {
                    lblStatus.Text = TextManager.GetString("DownloadingError");
                    btnRetry.Visible = true;
                }
            }
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* best effort cleanup */ }
        }

        private void LaunchFileSwap(string currentExe, string tempNewExe)
        {
            try
            {
                // Record which version we expect after the swap so the relaunched app
                // can verify the update actually applied before reporting success.
                UpdateMarker.Save(_expectedVersion);

                var currentExeDir = Path.GetDirectoryName(currentExe) ?? Path.GetTempPath();
                bool requiresElevation = false;
                try
                {
                    string testFile = Path.Combine(currentExeDir, $".perm_test_{Guid.NewGuid():N}");
                    File.WriteAllText(testFile, "");
                    File.Delete(testFile);
                }
                catch
                {
                    requiresElevation = true;
                }

                var scriptPath = Path.Combine(Path.GetTempPath(), $"kopolart-update-{Guid.NewGuid():N}.cmd");
                File.WriteAllText(scriptPath, BuildSwapScript(currentExe, tempNewExe));

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"\"{scriptPath}\"\"",
                    CreateNoWindow = !requiresElevation,
                    UseShellExecute = requiresElevation,
                    Verb = requiresElevation ? "runas" : "",
                    WorkingDirectory = Path.GetTempPath()
                });

                System.Windows.Forms.Application.Exit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start file swap: {ex}");
                CustomMessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
            }
        }

        /// <summary>
        /// Builds the swap script. Rather than deleting the old exe immediately, it first renames
        /// the current executable to .old (with retries for process termination). If moving the new
        /// executable into place fails, it safely rolls back by restoring the .old file, ensuring
        /// the user is never left without an executable.
        /// </summary>
        private static string BuildSwapScript(string currentExe, string tempNewExe)
        {
            string backupExe = currentExe + ".old";
            string currentExeFileName = Path.GetFileName(currentExe);
            string backupExeFileName = Path.GetFileName(backupExe);
            string psEscapedCurrentExe = currentExe.Replace("'", "''");

            return string.Join("\r\n",
                "@echo off",
                "setlocal enabledelayedexpansion",
                $"if exist \"{backupExe}\" del /f /q \"{backupExe}\" >nul 2>&1",
                "set /a tries=0",
                ":waitrename",
                $"ren \"{currentExe}\" \"{backupExeFileName}\" >nul 2>&1",
                $"if exist \"{currentExe}\" (",
                "    set /a tries+=1",
                "    if !tries! geq 60 goto restartold",
                "    ping -n 2 127.0.0.1 >nul",
                "    goto waitrename",
                ")",
                "set /a tries=0",
                ":trymove",
                $"move /y \"{tempNewExe}\" \"{currentExe}\" >nul 2>&1",
                $"if not exist \"{currentExe}\" (",
                "    set /a tries+=1",
                "    if !tries! geq 30 goto rollback",
                "    ping -n 2 127.0.0.1 >nul",
                "    goto trymove",
                ")",
                $"del /f /q \"{backupExe}\" >nul 2>&1",
                $"powershell -NoProfile -NonInteractive -Command \"Start-Process '{psEscapedCurrentExe}' -ArgumentList '--updated'\" 2>nul || start \"\" \"{currentExe}\" --updated",
                "goto cleanup",
                ":rollback",
                $"if not exist \"{currentExe}\" if exist \"{backupExe}\" ren \"{backupExe}\" \"{currentExeFileName}\" >nul 2>&1",
                ":restartold",
                $"if exist \"{currentExe}\" (powershell -NoProfile -NonInteractive -Command \"Start-Process '{psEscapedCurrentExe}'\" 2>nul || start \"\" \"{currentExe}\")",
                ":cleanup",
                $"del /f /q \"{tempNewExe}\" >nul 2>&1",
                "(goto) 2>nul & del \"%~f0\"",
                "");
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        private async void BtnRetry_Click(object? sender, EventArgs e)
        {
            btnRetry.Visible = false;
            lblStatus.Text = TextManager.GetString("DownloadingRestarting");
            progressBar.Value = 0;
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            await StartDownloadAsync(_cts.Token);
        }



        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Closing the dialog (X or Cancel) must always cancel the in-flight download,
            // otherwise the background task would later swap the executable without consent.
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed - nothing to cancel.
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}


