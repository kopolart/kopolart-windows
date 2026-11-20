using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controls;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class DownloadsDialog : ThemedDialog
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IWallpaperService _wallpaperService;
        private readonly Action<string>? _onDownloadUrl;
        private Label lblTitle = null!;
        private Panel pnlHeroCard = null!;
        private Label lblHeroHeader = null!;
        private CategoryBadgeLabel lblHeroBadge = null!;
        private Label lblHeroFileName = null!;
        private Label lblHeroDetails = null!;
        private TextBox txtHeroUrl = null!;
        private Button btnDownloadAgain = null!;
        private Button btnOpenFile = null!;
        private Button btnOpenFolder = null!;
        private Button btnCopyLink = null!;
        private Button btnSetWallpaper = null!;
        private ContextMenuStrip cmsDownloads = null!;
        private ToolStripMenuItem miSetWallpaper = null!;
        private Label lblHistoryTitle = null!;
        private ComboBox cmbCategoryFilter = null!;
        private ListView lstDownloads = null!;
        private Button btnClear = null!;
        private Button btnClose = null!;

        public DownloadsDialog(ISettingsManager settingsManager, bool isDark, IWallpaperService wallpaperService, Action<string>? onDownloadUrl = null)
            : base(isDark)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
            _onDownloadUrl = onDownloadUrl;

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            LoadDownloads();
            InitializeTheme();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            pnlHeroCard = new Panel();
            lblHeroHeader = new Label();
            lblHeroBadge = new CategoryBadgeLabel();
            lblHeroFileName = new Label();
            lblHeroDetails = new Label();
            txtHeroUrl = new TextBox();
            btnDownloadAgain = new Button();
            btnOpenFile = new Button();
            btnOpenFolder = new Button();
            btnCopyLink = new Button();
            btnSetWallpaper = new Button();
            cmsDownloads = new ContextMenuStrip();
            miSetWallpaper = new ToolStripMenuItem();

            lblHistoryTitle = new Label();
            cmbCategoryFilter = new ComboBox();
            lstDownloads = new ListView();
            btnClear = new Button();
            btnClose = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 580);
            MinimumSize = new Size(660, 520);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("DownloadsTitle");

            // Title (Right-aligned in RTL)
            lblTitle.Text = TextManager.GetString("DownloadsTitle");
            lblTitle.Font = FontManager.CreateBlackFont(13F);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTitle.Location = new Point(720 - 20 - lblTitle.PreferredWidth, 14);

            // Hero Card (Last Downloaded File)
            pnlHeroCard.Location = new Point(20, 48);
            pnlHeroCard.Size = new Size(680, 196);
            pnlHeroCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeroCard.Padding = new Padding(14, 12, 14, 12);
            pnlHeroCard.Resize += (s, e) => UpdateHeroCardLayout();

            lblHeroHeader.Text = TextManager.GetString("DownloadsLastDownloaded");
            lblHeroHeader.Font = FontManager.CreateFont(9F, FontStyle.Bold);
            lblHeroHeader.AutoSize = true;

            lblHeroBadge.Visible = false;

            lblHeroFileName.Font = FontManager.CreateBlackFont(10.5F);
            lblHeroFileName.Location = new Point(14, 40);
            lblHeroFileName.Size = new Size(652, 24);
            lblHeroFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblHeroDetails.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            lblHeroDetails.Location = new Point(14, 66);
            lblHeroDetails.Size = new Size(652, 18);
            lblHeroDetails.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            txtHeroUrl.Location = new Point(14, 88);
            txtHeroUrl.Size = new Size(652, 24);
            txtHeroUrl.ReadOnly = true;
            txtHeroUrl.RightToLeft = RightToLeft.No;
            txtHeroUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtHeroUrl.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            // Hero Action Buttons
            btnDownloadAgain.Text = TextManager.GetString("DownloadsDownloadAgain");
            btnDownloadAgain.Size = new Size(130, 28);
            btnDownloadAgain.Cursor = Cursors.Hand;
            btnDownloadAgain.FlatStyle = FlatStyle.Flat;
            btnDownloadAgain.FlatAppearance.BorderSize = 1;
            btnDownloadAgain.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            btnDownloadAgain.Click += BtnDownloadAgain_Click;

            btnOpenFile.Text = TextManager.GetString("DownloadsOpenFile");
            btnOpenFile.Size = new Size(110, 28);
            btnOpenFile.Cursor = Cursors.Hand;
            btnOpenFile.FlatStyle = FlatStyle.Flat;
            btnOpenFile.FlatAppearance.BorderSize = 1;
            btnOpenFile.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            btnOpenFile.Click += BtnOpenFile_Click;

            btnOpenFolder.Text = TextManager.GetString("DownloadsOpenFolder");
            btnOpenFolder.Size = new Size(125, 28);
            btnOpenFolder.Cursor = Cursors.Hand;
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.FlatAppearance.BorderSize = 1;
            btnOpenFolder.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            btnOpenFolder.Click += BtnOpenFolder_Click;

            btnCopyLink.Text = TextManager.GetString("DownloadsCopyLink");
            btnCopyLink.Size = new Size(105, 28);
            btnCopyLink.Cursor = Cursors.Hand;
            btnCopyLink.FlatStyle = FlatStyle.Flat;
            btnCopyLink.FlatAppearance.BorderSize = 1;
            btnCopyLink.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            btnCopyLink.Click += BtnCopyLink_Click;

            btnSetWallpaper.Text = TextManager.GetString("DownloadsSetWallpaper");
            btnSetWallpaper.Size = new Size(132, 28);
            btnSetWallpaper.Cursor = Cursors.Hand;
            btnSetWallpaper.FlatStyle = FlatStyle.Flat;
            btnSetWallpaper.FlatAppearance.BorderSize = 1;
            btnSetWallpaper.Font = FontManager.CreateFont(8.5F, FontStyle.Regular);
            btnSetWallpaper.Click += BtnSetWallpaper_Click;

            miSetWallpaper.Text = TextManager.GetString("DownloadsSetWallpaperMenu");
            miSetWallpaper.Click += (s, e) => SetWallpaperForSelected();

            cmsDownloads.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            cmsDownloads.Items.Add(miSetWallpaper);
            cmsDownloads.Opening += CmsDownloads_Opening;
            lstDownloads.ContextMenuStrip = cmsDownloads;

            pnlHeroCard.Controls.Add(lblHeroHeader);
            pnlHeroCard.Controls.Add(lblHeroBadge);
            pnlHeroCard.Controls.Add(lblHeroFileName);
            pnlHeroCard.Controls.Add(lblHeroDetails);
            pnlHeroCard.Controls.Add(txtHeroUrl);
            pnlHeroCard.Controls.Add(btnDownloadAgain);
            pnlHeroCard.Controls.Add(btnOpenFile);
            pnlHeroCard.Controls.Add(btnOpenFolder);
            pnlHeroCard.Controls.Add(btnCopyLink);
            pnlHeroCard.Controls.Add(btnSetWallpaper);

            UpdateHeroCardLayout();

            // History Section (Title on right, Category Filter on left)
            lblHistoryTitle.Text = TextManager.GetString("DownloadsHistory");
            lblHistoryTitle.Font = FontManager.CreateBlackFont(10.5F);
            lblHistoryTitle.AutoSize = true;
            lblHistoryTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblHistoryTitle.Location = new Point(720 - 20 - lblHistoryTitle.PreferredWidth, 258);

            cmbCategoryFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategoryFilter.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            cmbCategoryFilter.Size = new Size(160, 26);
            cmbCategoryFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            cmbCategoryFilter.Location = new Point(20, 254);
            cmbCategoryFilter.Items.AddRange(new object[]
            {
                TextManager.GetString("CategoryAll"),
                TextManager.GetString("CategoryWallpaper"),
                TextManager.GetString("CategoryPost"),
                TextManager.GetString("CategoryColor"),
                TextManager.GetString("CategoryOther")
            });
            cmbCategoryFilter.SelectedIndex = 0;
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => FilterDownloads();

            lstDownloads.Location = new Point(20, 286);
            lstDownloads.Size = new Size(680, 231);
            lstDownloads.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstDownloads.View = View.Details;
            lstDownloads.FullRowSelect = true;
            lstDownloads.MultiSelect = false;
            lstDownloads.GridLines = true;
            lstDownloads.OwnerDraw = true;
            lstDownloads.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lstDownloads.DrawColumnHeader += (s, e) => e.DrawDefault = true;
            lstDownloads.DrawItem += (s, e) => { };
            lstDownloads.DrawSubItem += LstDownloads_DrawSubItem;

            var colCategory = new ColumnHeader { Text = TextManager.GetString("DownloadsColType"), Width = 115 };
            var colName = new ColumnHeader { Text = TextManager.GetString("DownloadsColName"), Width = 190 };
            var colSize = new ColumnHeader { Text = TextManager.GetString("DownloadsColSize"), Width = 85, TextAlign = HorizontalAlignment.Right };
            var colDate = new ColumnHeader { Text = TextManager.GetString("DownloadsColDate"), Width = 140 };
            var colUrl = new ColumnHeader { Text = TextManager.GetString("DownloadsColUrl"), Width = 260 };

            lstDownloads.Columns.AddRange(new[] { colCategory, colName, colSize, colDate, colUrl });
            lstDownloads.DoubleClick += (s, e) => OpenSelectedDownload();

            // Bottom Buttons (Clear on right, Close on left)
            btnClear.Text = TextManager.GetString("DownloadsClearHistory");
            btnClear.Size = new Size(150, 32);
            btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClear.Location = new Point(720 - 150 - 20, 532);
            btnClear.Cursor = Cursors.Hand;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClear.Click += BtnClear_Click;

            btnClose.Text = TextManager.GetString("Close");
            btnClose.Size = new Size(96, 32);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnClose.Location = new Point(20, 532);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClose.Click += (s, e) => Close();

            Controls.Add(lblTitle);
            Controls.Add(pnlHeroCard);
            Controls.Add(lblHistoryTitle);
            Controls.Add(cmbCategoryFilter);
            Controls.Add(lstDownloads);
            Controls.Add(btnClear);
            Controls.Add(btnClose);

            CancelButton = btnClose;

            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadDownloads()
        {
            var history = _settingsManager.GetDownloadHistory();
            var last = _settingsManager.GetLastDownloadedItem();

            if (last != null)
            {
                lblHeroFileName.Text = last.FileName;
                string sizeStr = UsageTrackerService.FormatDataSize(last.FileSizeBytes);
                string dateStr = FormatTimestamp(last.DownloadedAt);
                lblHeroDetails.Text = $"{sizeStr}  -  {dateStr}";
                txtHeroUrl.Text = last.Url;

                lblHeroBadge.SetCategory(last.Category, IsDark);
                lblHeroBadge.Visible = true;

                btnDownloadAgain.Enabled = !string.IsNullOrWhiteSpace(last.Url);
                btnOpenFile.Enabled = File.Exists(last.FilePath);
                btnOpenFolder.Enabled = !string.IsNullOrWhiteSpace(last.FilePath);
                btnCopyLink.Enabled = !string.IsNullOrWhiteSpace(last.Url);
                btnSetWallpaper.Enabled = WallpaperEligibility.IsCandidate(last, File.Exists(last.FilePath));
            }
            else
            {
                lblHeroFileName.Text = TextManager.GetString("DownloadsNoDownloads");
                lblHeroDetails.Text = "";
                txtHeroUrl.Text = "";
                lblHeroBadge.Visible = false;

                btnDownloadAgain.Enabled = false;
                btnOpenFile.Enabled = false;
                btnOpenFolder.Enabled = false;
                btnCopyLink.Enabled = false;
                btnSetWallpaper.Enabled = false;
            }

            FilterDownloads();
            btnClear.Enabled = history != null && history.Count > 0;
            UpdateHeroCardLayout();
        }

        private void FilterDownloads()
        {
            var history = _settingsManager.GetDownloadHistory();
            int categoryIdx = cmbCategoryFilter.SelectedIndex;

            DownloadCategory? targetCategory = categoryIdx switch
            {
                1 => DownloadCategory.Wallpaper,
                2 => DownloadCategory.Post,
                3 => DownloadCategory.Color,
                4 => DownloadCategory.Other,
                _ => null
            };

            using var paintScope = lstDownloads.SuspendPainting();
            lstDownloads.Items.Clear();

            if (history != null && history.Count > 0)
            {
                foreach (var item in history.OrderByDescending(d => d.DownloadedAt))
                {
                    if (targetCategory.HasValue && item.Category != targetCategory.Value)
                        continue;

                    string sizeStr = UsageTrackerService.FormatDataSize(item.FileSizeBytes);
                    string dateStr = FormatTimestamp(item.DownloadedAt);
                    string typeStr = CategoryBadgeLabel.GetCategoryDisplayName(item.Category);

                    var lvi = new ListViewItem(typeStr);
                    lvi.SubItems.Add(item.FileName);
                    lvi.SubItems.Add(sizeStr);
                    lvi.SubItems.Add(dateStr);
                    lvi.SubItems.Add(item.Url);
                    lvi.Tag = item;

                    lstDownloads.Items.Add(lvi);
                }
            }
        }

        private string FormatTimestamp(DateTime dt)
        {
            return $"{PersianDateHelper.ToPersianDateString(dt)}\u060C {PersianNumberHelper.ToPersianDigits(dt.ToString("HH:mm:ss"))}";
        }

        private void BtnDownloadAgain_Click(object? sender, EventArgs e)
        {
            var last = _settingsManager.GetLastDownloadedItem();
            if (last != null && !string.IsNullOrWhiteSpace(last.Url))
            {
                _onDownloadUrl?.Invoke(last.Url);
                Close();
            }
        }

        private void BtnOpenFile_Click(object? sender, EventArgs e)
        {
            var last = _settingsManager.GetLastDownloadedItem();
            if (last != null && File.Exists(last.FilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(last.FilePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, ex.Message, TextManager.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
                }
            }
            else
            {
                CustomMessageBox.Show(this, TextManager.GetString("DownloadsFileNotFound"), TextManager.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning, IsDark);
            }
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            var last = _settingsManager.GetLastDownloadedItem();
            if (last != null && !string.IsNullOrWhiteSpace(last.FilePath))
            {
                try
                {
                    if (File.Exists(last.FilePath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{last.FilePath}\"");
                    }
                    else
                    {
                        string dir = Path.GetDirectoryName(last.FilePath) ?? "";
                        if (Directory.Exists(dir))
                        {
                            Process.Start("explorer.exe", $"\"{dir}\"");
                        }
                        else
                        {
                            CustomMessageBox.Show(this, TextManager.GetString("DownloadsFileNotFound"), TextManager.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning, IsDark);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, ex.Message, TextManager.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
                }
            }
        }

        private void BtnCopyLink_Click(object? sender, EventArgs e)
        {
            var last = _settingsManager.GetLastDownloadedItem();
            if (last != null && !string.IsNullOrWhiteSpace(last.Url))
            {
                try
                {
                    Clipboard.SetText(last.Url);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DownloadsDialog] Failed to copy URL to clipboard: {ex.Message}");
                }
            }
        }

        private void BtnSetWallpaper_Click(object? sender, EventArgs e)
        {
            SetWallpaperFor(_settingsManager.GetLastDownloadedItem());
        }

        private void SetWallpaperForSelected()
        {
            SetWallpaperFor(GetSelectedDownload());
        }

        private void SetWallpaperFor(DownloadedItem? item)
        {
            if (item == null)
                return;

            if (!WallpaperEligibility.IsCandidate(item, File.Exists(item.FilePath)))
            {
                CustomMessageBox.Show(
                    this,
                    TextManager.GetString("WallpaperNotEligible"),
                    TextManager.GetString("WallpaperSetTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    IsDark);
                return;
            }

            using var dialog = new WallpaperPreviewDialog(_wallpaperService, item.FilePath, IsDark);
            dialog.ShowDialog(this);
        }

        private DownloadedItem? GetSelectedDownload()
        {
            return lstDownloads.SelectedItems.Count > 0 && lstDownloads.SelectedItems[0].Tag is DownloadedItem item
                ? item
                : null;
        }

        private void CmsDownloads_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var item = GetSelectedDownload();
            miSetWallpaper.Enabled = WallpaperEligibility.IsCandidate(item, item != null && File.Exists(item.FilePath));
        }

        private void OpenSelectedDownload()
        {
            if (lstDownloads.SelectedItems.Count > 0 && lstDownloads.SelectedItems[0].Tag is DownloadedItem item)
            {
                if (File.Exists(item.FilePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show(this, ex.Message, TextManager.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(item.Url))
                {
                    _onDownloadUrl?.Invoke(item.Url);
                    Close();
                }
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            var confirmResult = CustomMessageBox.Show(this,
                TextManager.GetString("DownloadsClearConfirm"),
                TextManager.GetString("DownloadsClearHistory"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, IsDark);

            if (confirmResult == DialogResult.Yes)
            {
                _settingsManager.ClearDownloadHistory();
                LoadDownloads();
            }
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblHistoryTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                pnlHeroCard.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lblHeroHeader.ForeColor = Color.FromArgb(215, 175, 130);
                lblHeroFileName.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblHeroDetails.ForeColor = Color.FromArgb(170, 160, 150);
                txtHeroUrl.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                txtHeroUrl.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                lstDownloads.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lstDownloads.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                cmbCategoryFilter.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                cmbCategoryFilter.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                Color btnBg = Color.FromArgb(55, 55, 55);
                Color btnBorder = Color.FromArgb(75, 75, 75);
                Color btnFg = UIConstants.DarkMode.MenuStripForeColor;

                ApplyButtonStyle(btnDownloadAgain, btnBg, btnBorder, btnFg);
                ApplyButtonStyle(btnOpenFile, btnBg, btnBorder, btnFg);
                ApplyButtonStyle(btnOpenFolder, btnBg, btnBorder, btnFg);
                ApplyButtonStyle(btnCopyLink, btnBg, btnBorder, btnFg);
                ApplyButtonStyle(btnSetWallpaper, btnBg, btnBorder, btnFg);

                btnClear.BackColor = Color.Transparent;
                btnClear.ForeColor = UIConstants.DarkMode.DangerColor;
                btnClear.FlatAppearance.BorderColor = UIConstants.DarkMode.DangerColor;

                btnClose.BackColor = Color.FromArgb(60, 60, 60);
                btnClose.ForeColor = UIConstants.DarkMode.MenuStripForeColor;

                cmsDownloads.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                cmsDownloads.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
            }
            else
            {
                BackColor = Color.White;
                lblTitle.ForeColor = Color.FromArgb(40, 40, 40);
                lblHistoryTitle.ForeColor = Color.FromArgb(50, 45, 40);

                pnlHeroCard.BackColor = Color.FromArgb(250, 248, 245);
                lblHeroHeader.ForeColor = Color.FromArgb(160, 110, 60);
                lblHeroFileName.ForeColor = Color.FromArgb(40, 40, 40);
                lblHeroDetails.ForeColor = Color.FromArgb(110, 100, 90);
                txtHeroUrl.BackColor = Color.FromArgb(245, 243, 240);
                txtHeroUrl.ForeColor = Color.FromArgb(50, 50, 50);

                lstDownloads.BackColor = Color.White;
                lstDownloads.ForeColor = Color.FromArgb(30, 30, 30);

                cmbCategoryFilter.BackColor = Color.White;
                cmbCategoryFilter.ForeColor = Color.FromArgb(30, 30, 30);

                Color btnBg = Color.FromArgb(245, 240, 235);
                Color btnBorder = Color.FromArgb(215, 205, 195);
                Color btnFg = Color.FromArgb(60, 50, 40);

                ApplyButtonStyle(btnDownloadAgain, btnBg, btnBorder, btnFg);
                ApplyButtonStyle(btnOpenFile, btnBg, btnBorder, btnFg);
                ApplyButtonStyle(btnOpenFolder, btnBg, btnBorder, btnFg);
                ApplyButtonStyle(btnCopyLink, btnBg, btnBorder, btnFg);
                ApplyButtonStyle(btnSetWallpaper, btnBg, btnBorder, btnFg);

                btnClear.BackColor = Color.Transparent;
                btnClear.ForeColor = UIConstants.DangerColor;
                btnClear.FlatAppearance.BorderColor = UIConstants.DangerColor;

                btnClose.BackColor = Color.FromArgb(220, 215, 205);
                btnClose.ForeColor = Color.FromArgb(50, 50, 50);

                cmsDownloads.BackColor = Color.White;
                cmsDownloads.ForeColor = Color.FromArgb(30, 30, 30);
            }

            lblHeroBadge?.UpdateTheme(IsDark);

            pnlHeroCard.Paint -= HeroCard_Paint;
            pnlHeroCard.Paint += HeroCard_Paint;
            pnlHeroCard.Invalidate();
        }

        private void LstDownloads_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex != 4)
            {
                e.DrawDefault = true;
                return;
            }

            bool isSelected = e.Item?.Selected ?? false;
            if (isSelected)
            {
                e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
            }
            else
            {
                using var bgBrush = new SolidBrush(lstDownloads.BackColor);
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            Color textColor = isSelected
                ? SystemColors.HighlightText
                : SystemAccessibility.IsHighContrast
                    ? SystemColors.WindowText
                    : (IsDark ? Color.FromArgb(200, 200, 200) : Color.FromArgb(60, 60, 60));

            var flags = TextFormatFlags.Left |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.NoPrefix;

            var textRect = new Rectangle(
                e.Bounds.X + 4,
                e.Bounds.Y,
                Math.Max(0, e.Bounds.Width - 8),
                e.Bounds.Height);

            var font = e.SubItem?.Font ?? lstDownloads.Font;
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? string.Empty, font, textRect, textColor, flags);

            if (isSelected && (e.Item?.Focused ?? false))
            {
                e.DrawFocusRectangle(e.Bounds);
            }
        }

        private void HeroCard_Paint(object? sender, PaintEventArgs e)
        {
            Color border = SystemAccessibility.IsHighContrast
                ? SystemColors.ControlDark
                : IsDark ? UIConstants.DarkMode.CardBorderDark : Color.FromArgb(225, 218, 210);
            using var p = new Pen(border, 1);
            e.Graphics.DrawRectangle(p, 0, 0, pnlHeroCard.Width - 1, pnlHeroCard.Height - 1);
        }

        private static void ApplyButtonStyle(Button btn, Color bg, Color border, Color fg)
        {
            btn.BackColor = bg;
            btn.FlatAppearance.BorderColor = border;
            btn.ForeColor = fg;
        }

        private void UpdateHeroCardLayout()
        {
            if (pnlHeroCard == null || lblHeroHeader == null || lblHeroBadge == null || btnDownloadAgain == null)
                return;

            int cardWidth = pnlHeroCard.ClientSize.Width;
            int firstRowY = 120;
            int secondRowY = 154;

            lblHeroHeader.Location = new Point(cardWidth - 14 - lblHeroHeader.PreferredWidth, 12);
            lblHeroBadge.Location = new Point(lblHeroHeader.Left - 10 - lblHeroBadge.PreferredWidth, 9);

            int rx = cardWidth - 14 - btnDownloadAgain.Width;
            btnDownloadAgain.Location = new Point(rx, firstRowY);

            rx -= (btnOpenFile.Width + 10);
            btnOpenFile.Location = new Point(rx, firstRowY);

            rx -= (btnOpenFolder.Width + 10);
            btnOpenFolder.Location = new Point(rx, firstRowY);

            rx = cardWidth - 14 - btnCopyLink.Width;
            btnCopyLink.Location = new Point(rx, secondRowY);

            rx -= (btnSetWallpaper.Width + 10);
            btnSetWallpaper.Location = new Point(rx, secondRowY);
        }
    }
}

