using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    /// <summary>
    /// Shows a desktop-accurate preview and applies the downloaded wallpaper through
    /// <see cref="IWallpaperService"/>. All wallpaper logic lives in the service.
    /// </summary>
    public class WallpaperPreviewDialog : ThemedDialog
    {
        private readonly IWallpaperService _wallpaperService;
        private readonly string _imagePath;
        private readonly System.Windows.Forms.Timer _previewTimer;
        private readonly System.Windows.Forms.Timer _closeTimer;

        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Panel pnlPreview = null!;
        private PictureBox picPreview = null!;
        private Label lblPreviewMessage = null!;
        private Label lblMonitorCaption = null!;
        private ComboBox cmbMonitor = null!;
        private Label lblPlacementCaption = null!;
        private ComboBox cmbPlacement = null!;
        private Label lblImageInfo = null!;
        private Label lblHint = null!;
        private Label lblStatus = null!;
        private Button btnApply = null!;
        private Button btnCancel = null!;

        private Image? _image;
        private Bitmap? _previewBitmap;
        private List<WallpaperMonitorInfo> _monitors = new();
        private bool _isApplying;
        private bool _initialized;

        public WallpaperPreviewDialog(IWallpaperService wallpaperService, string imagePath, bool isDark)
            : base(isDark)
        {
            _wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
            _imagePath = imagePath ?? throw new ArgumentNullException(nameof(imagePath));

            _previewTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _previewTimer.Tick += PreviewTimer_Tick;

            _closeTimer = new System.Windows.Forms.Timer { Interval = 1400 };
            _closeTimer.Tick += CloseTimer_Tick;

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            LoadMonitors();
            LoadImage();
            InitializeTheme();
            UpdateHints();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblSubtitle = new Label();
            pnlPreview = new Panel();
            picPreview = new PictureBox();
            lblPreviewMessage = new Label();
            lblMonitorCaption = new Label();
            cmbMonitor = new ComboBox();
            lblPlacementCaption = new Label();
            cmbPlacement = new ComboBox();
            lblImageInfo = new Label();
            lblHint = new Label();
            lblStatus = new Label();
            btnApply = new Button();
            btnCancel = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 620);
            MinimumSize = new Size(820, 560);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("WallpaperSetTitle");

            lblTitle.Text = TextManager.GetString("WallpaperSetTitle");
            lblTitle.Font = FontManager.CreateBlackFont(14F);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTitle.Location = new Point(900 - 20 - lblTitle.PreferredWidth, 15);

            lblSubtitle.Text = TextManager.GetString("WallpaperSetSubtitle");
            lblSubtitle.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lblSubtitle.RightToLeft = RightToLeft.Yes;
            lblSubtitle.AutoSize = true;
            lblSubtitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSubtitle.Location = new Point(900 - 20 - lblSubtitle.PreferredWidth, 46);

            // Preview frame
            pnlPreview.Location = new Point(20, 80);
            pnlPreview.Size = new Size(600, 520);
            pnlPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlPreview.BackColor = Color.Black;
            pnlPreview.Paint += PreviewPanel_Paint;

            picPreview.Location = new Point(2, 2);
            picPreview.Size = new Size(596, 516);
            picPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            picPreview.BackColor = Color.Black;
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.SizeChanged += (s, e) => SchedulePreviewUpdate();

            lblPreviewMessage.Text = TextManager.GetString("WallpaperPreviewUnavailable");
            lblPreviewMessage.Dock = DockStyle.Fill;
            lblPreviewMessage.BackColor = Color.Black;
            lblPreviewMessage.ForeColor = Color.Gainsboro;
            lblPreviewMessage.TextAlign = ContentAlignment.MiddleCenter;
            lblPreviewMessage.Font = FontManager.CreateFont(10F, FontStyle.Bold);
            lblPreviewMessage.Visible = false;

            pnlPreview.Controls.Add(lblPreviewMessage);
            pnlPreview.Controls.Add(picPreview);
            lblPreviewMessage.BringToFront();

            // Settings column (right side in RTL)
            int columnX = 900 - 20 - 240;

            lblMonitorCaption.Text = TextManager.GetString("WallpaperMonitorCaption");
            lblMonitorCaption.Font = FontManager.CreateFont(9F, FontStyle.Bold);
            lblMonitorCaption.AutoSize = false;
            lblMonitorCaption.Size = new Size(240, 20);
            lblMonitorCaption.TextAlign = ContentAlignment.MiddleRight;
            lblMonitorCaption.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMonitorCaption.Location = new Point(columnX, 84);

            cmbMonitor.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMonitor.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            cmbMonitor.Size = new Size(240, 28);
            cmbMonitor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbMonitor.Location = new Point(columnX, 106);

            lblPlacementCaption.Text = TextManager.GetString("WallpaperPlacementCaption");
            lblPlacementCaption.Font = FontManager.CreateFont(9F, FontStyle.Bold);
            lblPlacementCaption.AutoSize = false;
            lblPlacementCaption.Size = new Size(240, 20);
            lblPlacementCaption.TextAlign = ContentAlignment.MiddleRight;
            lblPlacementCaption.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblPlacementCaption.Location = new Point(columnX, 150);

            cmbPlacement.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPlacement.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            cmbPlacement.Size = new Size(240, 28);
            cmbPlacement.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbPlacement.Location = new Point(columnX, 172);
            cmbPlacement.Items.AddRange(new object[]
            {
                TextManager.GetString("WallpaperPlacementFill"),
                TextManager.GetString("WallpaperPlacementFit"),
                TextManager.GetString("WallpaperPlacementStretch"),
                TextManager.GetString("WallpaperPlacementCenter"),
                TextManager.GetString("WallpaperPlacementTile"),
                TextManager.GetString("WallpaperPlacementSpan")
            });
            cmbPlacement.SelectedIndex = 0;

            lblImageInfo.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lblImageInfo.AutoSize = false;
            lblImageInfo.Size = new Size(240, 44);
            lblImageInfo.TextAlign = ContentAlignment.MiddleRight;
            lblImageInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblImageInfo.AutoEllipsis = true;
            lblImageInfo.Location = new Point(columnX, 216);

            lblHint.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            lblHint.AutoSize = false;
            lblHint.Size = new Size(240, 96);
            lblHint.TextAlign = ContentAlignment.MiddleRight;
            lblHint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblHint.Location = new Point(columnX, 266);

            lblStatus.Font = FontManager.CreateFont(9F, FontStyle.Bold);
            lblStatus.AutoSize = false;
            lblStatus.Size = new Size(240, 120);
            lblStatus.TextAlign = ContentAlignment.MiddleRight;
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblStatus.Location = new Point(columnX, 376);

            btnCancel.Text = TextManager.GetString("Cancel");
            btnCancel.Size = new Size(240, 36);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(columnX, 512);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnCancel.Click += (s, e) => Close();

            btnApply.Text = TextManager.GetString("WallpaperApplyButton");
            btnApply.Size = new Size(240, 44);
            btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnApply.Location = new Point(columnX, 556);
            btnApply.Cursor = Cursors.Hand;
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Font = FontManager.CreateBlackFont(10F);
            btnApply.Click += BtnApply_Click;

            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(pnlPreview);
            Controls.Add(lblMonitorCaption);
            Controls.Add(cmbMonitor);
            Controls.Add(lblPlacementCaption);
            Controls.Add(cmbPlacement);
            Controls.Add(lblImageInfo);
            Controls.Add(lblHint);
            Controls.Add(lblStatus);
            Controls.Add(btnCancel);
            Controls.Add(btnApply);

            CancelButton = btnCancel;

            cmbMonitor.SelectedIndexChanged += Monitor_Changed;
            cmbPlacement.SelectedIndexChanged += Placement_Changed;

            _initialized = true;

            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadMonitors()
        {
            try
            {
                _monitors = _wallpaperService.GetMonitors().ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperPreviewDialog] Monitor enumeration failed: {ex.Message}");
                _monitors = new List<WallpaperMonitorInfo>();
            }

            cmbMonitor.Items.Add(TextManager.GetString("WallpaperMonitorAll"));

            for (int index = 0; index < _monitors.Count; index++)
            {
                var monitor = _monitors[index];
                string number = PersianNumberHelper.ToPersianDigits(index + 1);
                string dimensions = $"{PersianNumberHelper.ToPersianDigits(monitor.Width)} × {PersianNumberHelper.ToPersianDigits(monitor.Height)}";
                cmbMonitor.Items.Add(string.Format(TextManager.GetString("WallpaperMonitorItem"), number, dimensions));
            }

            cmbMonitor.SelectedIndex = 0;
        }

        private void LoadImage()
        {
            try
            {
                _image = Image.FromFile(_imagePath);

                string dimensions = $"{PersianNumberHelper.ToPersianDigits(_image.Width)} × {PersianNumberHelper.ToPersianDigits(_image.Height)}";
                lblImageInfo.Text = string.Format(TextManager.GetString("WallpaperImageInfo"), dimensions, Path.GetFileName(_imagePath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperPreviewDialog] Image load failed: {ex.Message}");
                lblPreviewMessage.Visible = true;
                btnApply.Enabled = false;
                ShowStatus(TextManager.GetString("WallpaperUnsupportedFormat"), isError: true);
            }
        }

        private WallpaperPlacement GetSelectedPlacement()
        {
            int index = cmbPlacement.SelectedIndex;
            if (index < 0 || index > (int)WallpaperPlacement.Span)
                return WallpaperPlacement.Fill;

            return (WallpaperPlacement)index;
        }

        private WallpaperMonitorInfo? GetSelectedMonitor()
        {
            int index = cmbMonitor.SelectedIndex;
            if (index > 0 && index <= _monitors.Count)
                return _monitors[index - 1];

            return _monitors.FirstOrDefault(m => m.IsPrimary) ?? _monitors.FirstOrDefault();
        }

        private double GetPreviewAspect()
        {
            var monitor = GetSelectedMonitor();
            return monitor?.AspectRatio ?? 16.0 / 9.0;
        }

        private void UpdateHints()
        {
            if (_image == null)
            {
                lblHint.Text = string.Empty;
                return;
            }

            var hints = new List<string>();

            if (_image.Height > _image.Width)
                hints.Add(TextManager.GetString("WallpaperPortraitHint"));

            var monitor = GetSelectedMonitor();
            if (monitor != null &&
                monitor.Width > 0 &&
                monitor.Height > 0 &&
                (_image.Width < monitor.Width || _image.Height < monitor.Height))
            {
                hints.Add(TextManager.GetString("WallpaperSmallImageHint"));
            }

            lblHint.Text = string.Join(Environment.NewLine, hints);
        }

        private void UpdatePreview()
        {
            picPreview.Image = null;
            _previewBitmap?.Dispose();
            _previewBitmap = null;

            if (_image == null)
                return;

            var size = picPreview.ClientSize;
            if (size.Width < 16 || size.Height < 16)
                return;

            _previewBitmap = WallpaperPreviewRenderer.Render(_image, size, GetPreviewAspect(), GetSelectedPlacement());
            picPreview.Image = _previewBitmap;
        }

        private void SchedulePreviewUpdate()
        {
            if (!_initialized)
                return;

            _previewTimer.Stop();
            _previewTimer.Start();
        }

        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            _previewTimer.Stop();
            UpdatePreview();
        }

        private void Monitor_Changed(object? sender, EventArgs e)
        {
            if (!_initialized)
                return;

            UpdateHints();
            SchedulePreviewUpdate();
        }

        private void Placement_Changed(object? sender, EventArgs e)
        {
            if (!_initialized)
                return;

            bool span = GetSelectedPlacement() == WallpaperPlacement.Span;
            if (span && cmbMonitor.SelectedIndex != 0)
                cmbMonitor.SelectedIndex = 0;

            cmbMonitor.Enabled = !span && !_isApplying;

            UpdateHints();
            SchedulePreviewUpdate();
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            if (_isApplying || _image == null)
                return;

            ApplyWallpaper();
        }

        private void ApplyWallpaper()
        {
            var placement = GetSelectedPlacement();
            string? monitorId = null;

            if (placement != WallpaperPlacement.Span &&
                cmbMonitor.SelectedIndex > 0 &&
                cmbMonitor.SelectedIndex <= _monitors.Count)
            {
                monitorId = _monitors[cmbMonitor.SelectedIndex - 1].Id;
            }

            SetApplying(true);
            WallpaperSetResult result;
            try
            {
                result = _wallpaperService.SetWallpaper(_imagePath, monitorId, placement);
            }
            finally
            {
                SetApplying(false);
            }

            if (result.Succeeded)
            {
                ShowStatus(TextManager.GetString("WallpaperApplySuccess"), isError: false);
                btnApply.Enabled = false;
                _closeTimer.Start();
                return;
            }

            ShowStatus(DescribeFailure(result.Status), isError: true);
        }

        private static string DescribeFailure(WallpaperSetStatus status)
        {
            return status switch
            {
                WallpaperSetStatus.FileMissing => TextManager.GetString("WallpaperFileMissing"),
                WallpaperSetStatus.UnsupportedFormat => TextManager.GetString("WallpaperUnsupportedFormat"),
                WallpaperSetStatus.ConversionFailed => TextManager.GetString("WallpaperConversionFailed"),
                _ => TextManager.GetString("WallpaperApplyFailed")
            };
        }

        private void SetApplying(bool applying)
        {
            _isApplying = applying;
            btnApply.Enabled = !applying && _image != null;
            btnApply.Text = applying ? TextManager.GetString("WallpaperApplying") : TextManager.GetString("WallpaperApplyButton");
            cmbPlacement.Enabled = !applying;
            cmbMonitor.Enabled = !applying && GetSelectedPlacement() != WallpaperPlacement.Span;
            Refresh();
        }

        private void ShowStatus(string message, bool isError)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = isError
                ? (IsDark ? Color.FromArgb(255, 138, 128) : UIConstants.DangerColor)
                : (IsDark ? UIConstants.LiveGreenDark : UIConstants.LiveGreenLight);
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            _closeTimer.Stop();
            if (!IsDisposed)
                Close();
        }

        private void PreviewPanel_Paint(object? sender, PaintEventArgs e)
        {
            Color border = IsDark ? UIConstants.DarkMode.CardBorderDark : Color.FromArgb(225, 218, 210);
            using var pen = new Pen(border);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlPreview.Width - 1, pnlPreview.Height - 1);
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblSubtitle.ForeColor = Color.FromArgb(180, 170, 160);
                lblMonitorCaption.ForeColor = Color.FromArgb(215, 175, 130);
                lblPlacementCaption.ForeColor = Color.FromArgb(215, 175, 130);
                lblImageInfo.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblHint.ForeColor = Color.FromArgb(215, 175, 130);

                cmbMonitor.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                cmbMonitor.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                cmbPlacement.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                cmbPlacement.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                btnApply.BackColor = UIConstants.BrandPrimaryLight;
                btnApply.ForeColor = Color.FromArgb(74, 59, 42);

                btnCancel.BackColor = Color.FromArgb(55, 55, 55);
                btnCancel.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                btnCancel.FlatAppearance.BorderColor = Color.FromArgb(75, 75, 75);
            }
            else
            {
                BackColor = Color.White;
                lblTitle.ForeColor = Color.FromArgb(40, 40, 40);
                lblSubtitle.ForeColor = Color.FromArgb(110, 100, 90);
                lblMonitorCaption.ForeColor = Color.FromArgb(160, 110, 60);
                lblPlacementCaption.ForeColor = Color.FromArgb(160, 110, 60);
                lblImageInfo.ForeColor = Color.FromArgb(40, 40, 40);
                lblHint.ForeColor = Color.FromArgb(160, 110, 60);

                cmbMonitor.BackColor = Color.White;
                cmbMonitor.ForeColor = Color.FromArgb(30, 30, 30);
                cmbPlacement.BackColor = Color.White;
                cmbPlacement.ForeColor = Color.FromArgb(30, 30, 30);

                btnApply.BackColor = UIConstants.BrandPrimary;
                btnApply.ForeColor = Color.White;

                btnCancel.BackColor = Color.FromArgb(245, 240, 235);
                btnCancel.ForeColor = Color.FromArgb(60, 50, 40);
                btnCancel.FlatAppearance.BorderColor = Color.FromArgb(215, 205, 195);
            }

            lblStatus.ForeColor = IsDark ? Color.FromArgb(180, 170, 160) : Color.FromArgb(110, 100, 90);

            pnlPreview.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewTimer?.Dispose();
                _closeTimer?.Dispose();

                if (picPreview != null)
                    picPreview.Image = null;

                _previewBitmap?.Dispose();
                _previewBitmap = null;

                _image?.Dispose();
                _image = null;
            }

            base.Dispose(disposing);
        }
    }
}
