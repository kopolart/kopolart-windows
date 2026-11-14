using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using Microsoft.Web.WebView2.Core;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class DownloadProgressDialog : ThemedDialog
    {
        private readonly CoreWebView2DownloadOperation _downloadOperation;

        private Label lblFileName = null!;
        private Label lblStatus = null!;
        private ProgressBar progressBar = null!;
        private Button btnCancel = null!;
        private Button btnOpenFolder = null!;

        public DownloadProgressDialog(CoreWebView2DownloadOperation downloadOperation, bool isDark)
            : base(isDark)
        {
            _downloadOperation = downloadOperation ?? throw new ArgumentNullException(nameof(downloadOperation));

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            InitializeTheme();

            // Wire events
            _downloadOperation.BytesReceivedChanged += DownloadOperation_BytesReceivedChanged;
            _downloadOperation.StateChanged += DownloadOperation_StateChanged;

            UpdateProgress();
        }

        private void InitializeComponent()
        {
            lblFileName = new Label();
            lblStatus = new Label();
            progressBar = new ProgressBar();
            btnCancel = new Button();
            btnOpenFolder = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(400, 160);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.Manual;
            Text = TextManager.GetString("DownloadTitle");

            lblFileName.Font = FontManager.CreateBlackFont(9.5F);
            lblFileName.Location = new Point(20, 20);
            lblFileName.Size = new Size(360, 20);
            lblFileName.Text = Path.GetFileName(_downloadOperation.ResultFilePath);
            lblFileName.AutoEllipsis = true;

            progressBar.Location = new Point(20, 50);
            progressBar.Size = new Size(360, 23);

            lblStatus.Location = new Point(20, 80);
            lblStatus.Size = new Size(360, 20);
            lblStatus.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lblStatus.Text = "";

            btnCancel.Text = TextManager.GetString("Cancel");
            btnCancel.Size = new Size(90, 30);
            btnCancel.Location = new Point(290, 110);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnCancel.Click += BtnCancel_Click;

            btnOpenFolder.Text = TextManager.GetString("OpenFolder");
            btnOpenFolder.Size = new Size(110, 30);
            btnOpenFolder.Location = new Point(170, 110);
            btnOpenFolder.Cursor = Cursors.Hand;
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            btnOpenFolder.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnOpenFolder.Visible = false;
            btnOpenFolder.Click += BtnOpenFolder_Click;

            Controls.Add(lblFileName);
            Controls.Add(progressBar);
            Controls.Add(lblStatus);
            Controls.Add(btnCancel);
            Controls.Add(btnOpenFolder);

            CancelButton = btnCancel;

            ResumeLayout(false);
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblFileName.ForeColor = UIConstants.DarkMode.ThemeColor;
                lblStatus.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnCancel.BackColor = Color.FromArgb(60, 60, 60);
                btnCancel.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnOpenFolder.BackColor = UIConstants.DarkMode.ThemeColor;
                btnOpenFolder.ForeColor = Color.White;
                return;
            }

            BackColor = Color.White;
            lblFileName.ForeColor = Color.FromArgb(74, 59, 42);
            lblStatus.ForeColor = Color.FromArgb(85, 85, 85);
            btnCancel.BackColor = Color.FromArgb(240, 240, 240);
            btnCancel.ForeColor = Color.FromArgb(85, 85, 85);
            btnOpenFolder.BackColor = UIConstants.ThemeColor;
            btnOpenFolder.ForeColor = Color.FromArgb(74, 59, 42);
        }

        private void DownloadOperation_BytesReceivedChanged(object? sender, object e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(UpdateProgress)); } catch { }
            }
            else
            {
                UpdateProgress();
            }
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(HandleStateChanged)); } catch { }
            }
            else
            {
                HandleStateChanged();
            }
        }

        private void UpdateProgress()
        {
            try
            {
                if (IsDisposed) return;

                long received = _downloadOperation.BytesReceived;
                ulong? total = _downloadOperation.TotalBytesToReceive;

                double receivedMb = received / (1024.0 * 1024.0);

                if (total.HasValue && total.Value > 0)
                {
                    double totalMb = total.Value / (1024.0 * 1024.0);
                    int percent = (int)((double)received / total.Value * 100);
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = Math.Clamp(percent, 0, 100);

                    string format = TextManager.GetString("DownloadProgress");
                    lblStatus.Text = string.Format(format, percent, receivedMb.ToString("0.00"), totalMb.ToString("0.00"));
                }
                else
                {
                    progressBar.Style = ProgressBarStyle.Marquee;
                    lblStatus.Text = $"در حال دانلود: {PersianNumberHelper.ToPersianDigits(receivedMb.ToString("0.00"))} مگابایت";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating download progress: {ex.Message}");
            }
        }

        private void HandleStateChanged()
        {
            if (IsDisposed) return;

            var state = _downloadOperation.State;
            if (state == CoreWebView2DownloadState.Completed)
            {
                progressBar.Value = 100;
                lblStatus.Text = TextManager.GetString("DownloadComplete");
                btnCancel.Text = TextManager.GetString("Close");
                btnCancel.Click -= BtnCancel_Click;
                btnCancel.Click += (s, e) => Close();
                
                btnOpenFolder.Visible = true;
            }
            else if (state == CoreWebView2DownloadState.Interrupted)
            {
                lblStatus.Text = TextManager.GetString("DownloadFailed");
                btnCancel.Text = TextManager.GetString("Close");
                btnCancel.Click -= BtnCancel_Click;
                btnCancel.Click += (s, e) => Close();
            }
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            try
            {
                string dir = Path.GetDirectoryName(_downloadOperation.ResultFilePath) ?? "";
                if (Directory.Exists(dir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_downloadOperation.ResultFilePath}\"");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open folder: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_downloadOperation.State == CoreWebView2DownloadState.InProgress)
                {
                    _downloadOperation.Cancel();
                    lblStatus.Text = TextManager.GetString("DownloadCancelled");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cancel download: {ex.Message}");
            }
            Close();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Owner != null)
            {
                int x = Owner.Location.X + (Owner.Width - Width) / 2;
                int y = Owner.Location.Y + (Owner.Height - Height) / 2;
                Location = new Point(x, y);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _downloadOperation.BytesReceivedChanged -= DownloadOperation_BytesReceivedChanged;
            _downloadOperation.StateChanged -= DownloadOperation_StateChanged;
            base.OnFormClosing(e);
        }
    }
}


