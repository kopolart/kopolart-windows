using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controls;
using Kopolart.Windows.UI.Helpers;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class HistoryDialog : ThemedDialog
    {
        private static readonly CultureInfo PersianCulture = CreatePersianCulture();

        private readonly ISettingsManager _settingsManager;
        private readonly Action<string> _onNavigate;

        private Label lblTitle = null!;
        private ComboBox cmbCategoryFilter = null!;
        private ListView lstHistory = null!;
        private CategoryBadgeLabel lblSelectedBadge = null!;
        private TextBox txtSelectedUrl = null!;
        private Button btnNavigate = null!;
        private Button btnClear = null!;
        private Button btnClose = null!;

        public HistoryDialog(ISettingsManager settingsManager, bool isDark, Action<string> onNavigate)
            : base(isDark)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _onNavigate = onNavigate ?? throw new ArgumentNullException(nameof(onNavigate));

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            LoadHistory();
            InitializeTheme();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            cmbCategoryFilter = new ComboBox();
            lstHistory = new ListView();
            lblSelectedBadge = new CategoryBadgeLabel();
            txtSelectedUrl = new TextBox();
            btnNavigate = new Button();
            btnClear = new Button();
            btnClose = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(660, 500);
            MinimumSize = new Size(580, 420);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("HistoryTitle");

            lblTitle.Text = TextManager.GetString("HistoryTitle");
            lblTitle.Font = FontManager.CreateBlackFont(14F);
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTitle.Location = new Point(660 - 20 - lblTitle.PreferredWidth, 15);

            // Category Filter Dropdown (Placed on the left in RTL to prevent collision with Title)
            cmbCategoryFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategoryFilter.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            cmbCategoryFilter.Size = new Size(160, 26);
            cmbCategoryFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            cmbCategoryFilter.Location = new Point(20, 15);
            cmbCategoryFilter.Items.AddRange(new object[]
            {
                TextManager.GetString("CategoryAll"),
                TextManager.GetString("CategoryWallpaper"),
                TextManager.GetString("CategoryPost"),
                TextManager.GetString("CategoryColor"),
                TextManager.GetString("CategoryTools"),
                TextManager.GetString("CategoryOther")
            });
            cmbCategoryFilter.SelectedIndex = 0;
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => FilterHistory();

            lstHistory.Location = new Point(20, 55);
            lstHistory.Size = new Size(620, 330);
            lstHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstHistory.View = View.Details;
            lstHistory.FullRowSelect = true;
            lstHistory.MultiSelect = false;
            lstHistory.GridLines = true;
            lstHistory.Scrollable = true;
            lstHistory.ShowItemToolTips = true;
            lstHistory.OwnerDraw = true;
            lstHistory.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lstHistory.DrawColumnHeader += (s, e) => e.DrawDefault = true;
            lstHistory.DrawItem += (s, e) => { };
            lstHistory.DrawSubItem += LstHistory_DrawSubItem;

            var colCategory = new ColumnHeader
            {
                Text = TextManager.GetString("HistoryColCategory"),
                Width = 95
            };
            var colTitle = new ColumnHeader
            {
                Text = TextManager.GetString("HistoryColTitle"),
                Width = 260
            };
            var colDate = new ColumnHeader
            {
                Text = TextManager.GetString("HistoryColVisitedAt"),
                Width = 140,
                TextAlign = HorizontalAlignment.Right
            };
            var colUrl = new ColumnHeader
            {
                Text = TextManager.GetString("HistoryColUrl"),
                Width = 380,
                TextAlign = HorizontalAlignment.Left
            };
            lstHistory.Columns.AddRange(new[] { colCategory, colTitle, colDate, colUrl });

            lstHistory.DoubleClick += (s, e) => NavigateSelected();
            lstHistory.SelectedIndexChanged += LstHistory_SelectedIndexChanged;

            lblSelectedBadge.Visible = false;

            txtSelectedUrl.Location = new Point(20, 395);
            txtSelectedUrl.Size = new Size(620, 24);
            txtSelectedUrl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtSelectedUrl.ReadOnly = true;
            txtSelectedUrl.RightToLeft = RightToLeft.No;
            txtSelectedUrl.Font = FontManager.CreateFont(9F, FontStyle.Regular);

            // Action Buttons: Navigate & Clear on right, Close on left
            btnNavigate.Text = TextManager.GetString("HistoryOpenPage");
            btnNavigate.Size = new Size(120, 35);
            btnNavigate.Location = new Point(660 - 20 - 120, 435);
            btnNavigate.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnNavigate.Cursor = Cursors.Hand;
            btnNavigate.FlatStyle = FlatStyle.Flat;
            btnNavigate.FlatAppearance.BorderSize = 1;
            btnNavigate.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnNavigate.Click += (s, e) => NavigateSelected();

            btnClear.Text = TextManager.GetString("ClearHistory");
            btnClear.Size = new Size(120, 35);
            btnClear.Location = new Point(660 - 20 - 120 - 10 - 120, 435);
            btnClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClear.Cursor = Cursors.Hand;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClear.Click += BtnClear_Click;

            btnClose.Text = TextManager.GetString("Close");
            btnClose.Size = new Size(100, 35);
            btnClose.Location = new Point(20, 435);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClose.Click += (s, e) => Close();

            Controls.Add(lblTitle);
            Controls.Add(cmbCategoryFilter);
            Controls.Add(lstHistory);
            Controls.Add(lblSelectedBadge);
            Controls.Add(txtSelectedUrl);
            Controls.Add(btnNavigate);
            Controls.Add(btnClear);
            Controls.Add(btnClose);

            AcceptButton = btnNavigate;
            CancelButton = btnClose;

            Resize += (s, e) => UpdateSelectedUrlLayout();

            ResumeLayout(false);
            PerformLayout();
        }

        private void UpdateSelectedUrlLayout()
        {
            if (txtSelectedUrl == null || lblSelectedBadge == null) return;
            int clientWidth = ClientSize.Width;
            int y = 395;
            int height = 24;

            if (lblSelectedBadge.Visible)
            {
                int badgeWidth = lblSelectedBadge.PreferredWidth;

                lblSelectedBadge.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                lblSelectedBadge.Location = new Point(clientWidth - 20 - badgeWidth, y);
                txtSelectedUrl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                txtSelectedUrl.Location = new Point(20, y);
                txtSelectedUrl.Size = new Size(clientWidth - 20 - badgeWidth - 10 - 20, height);
            }
            else
            {
                txtSelectedUrl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                txtSelectedUrl.Location = new Point(20, y);
                txtSelectedUrl.Size = new Size(clientWidth - 40, height);
            }
        }

        private void LoadHistory()
        {
            FilterHistory();
        }

        private void FilterHistory()
        {
            lstHistory.Items.Clear();
            txtSelectedUrl.Text = string.Empty;
            lblSelectedBadge.Visible = false;
            UpdateSelectedUrlLayout();

            var history = _settingsManager.GetHistory();
            int categoryIdx = cmbCategoryFilter.SelectedIndex;

            ContentCategory? targetCategory = categoryIdx switch
            {
                1 => ContentCategory.Wallpaper,
                2 => ContentCategory.Post,
                3 => ContentCategory.Color,
                4 => ContentCategory.Tools,
                5 => ContentCategory.Other,
                _ => null
            };

            if (history.Count == 0)
            {
                var emptyItem = new ListViewItem(TextManager.GetString("NoHistory"));
                lstHistory.Items.Add(emptyItem);
                btnNavigate.Enabled = false;
                return;
            }

            btnNavigate.Enabled = false;
            using var paintScope = lstHistory.SuspendPainting();
            foreach (var h in history)
            {
                var cat = ContentCategoryHelper.DetermineCategory(h.Url);
                if (targetCategory.HasValue && cat != targetCategory.Value)
                    continue;

                bool isBookmarked = _settingsManager.IsBookmarked(h.Url);
                string displayTitle = (isBookmarked ? "★ " : "") + (string.IsNullOrWhiteSpace(h.Title) ? h.Url : h.Title);
                string visitedDate = FormatVisitedDate(h.VisitedAt);
                string badgeName = CategoryBadgeLabel.GetCategoryDisplayName(cat);

                var item = new ListViewItem(badgeName);
                item.SubItems.Add(displayTitle);
                item.SubItems.Add(visitedDate);
                item.SubItems.Add(h.Url);
                item.Tag = h;
                item.ToolTipText = $"[{badgeName}] {displayTitle}\r\n{h.Url}";
                lstHistory.Items.Add(item);
            }
        }

        private void LstHistory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstHistory.SelectedItems.Count > 0 && lstHistory.SelectedItems[0].Tag is VisitedPage page)
            {
                txtSelectedUrl.Text = page.Url;
                var cat = ContentCategoryHelper.DetermineCategory(page.Url);
                lblSelectedBadge.SetCategory(cat, IsDark);
                lblSelectedBadge.Visible = true;
                UpdateSelectedUrlLayout();
                btnNavigate.Enabled = true;
            }
            else
            {
                txtSelectedUrl.Text = string.Empty;
                lblSelectedBadge.Visible = false;
                UpdateSelectedUrlLayout();
                btnNavigate.Enabled = false;
            }
        }

        private static CultureInfo CreatePersianCulture()
        {
            var ci = (CultureInfo)CultureInfo.GetCultureInfo("fa-IR").Clone();
            ci.DateTimeFormat.Calendar = new PersianCalendar();
            return ci;
        }

        private string FormatVisitedDate(DateTime date)
        {
            string formatted = date.ToString("yyyy/MM/dd HH:mm", PersianCulture);
            return PersianNumberHelper.ToPersianDigits(formatted);
        }

        private void LstHistory_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            bool isBookmarked = e.Item?.Tag is VisitedPage page && _settingsManager.IsBookmarked(page.Url);

            if (e.ColumnIndex != 3 && !isBookmarked)
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
                using var bgBrush = new SolidBrush(lstHistory.BackColor);
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            Color textColor = isSelected
                ? SystemColors.HighlightText
                : SystemAccessibility.IsHighContrast
                    ? SystemColors.WindowText
                    : e.ColumnIndex switch
                    {
                        0 => IsDark ? UIConstants.BrandPrimaryLight : UIConstants.BrandPrimary,
                        1 => IsDark ? Color.White : Color.FromArgb(30, 30, 30),
                        2 => IsDark ? Color.FromArgb(170, 170, 170) : Color.FromArgb(90, 90, 90),
                        _ => IsDark ? Color.FromArgb(200, 200, 200) : Color.FromArgb(60, 60, 60)
                    };

            var flags = (e.ColumnIndex == 2 ? TextFormatFlags.Right : TextFormatFlags.Left) |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.SingleLine |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.NoPrefix;

            var textRect = new Rectangle(
                e.Bounds.X + 4,
                e.Bounds.Y,
                Math.Max(0, e.Bounds.Width - 8),
                e.Bounds.Height);

            var font = e.SubItem?.Font ?? lstHistory.Font;
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? string.Empty, font, textRect, textColor, flags);

            if (isBookmarked)
            {
                Color borderColor = SystemAccessibility.IsHighContrast ? SystemColors.Highlight : UIConstants.BrandGold;
                using var goldPen = new Pen(borderColor, 1.5f);
                int top = e.Bounds.Top;
                int bottom = e.Bounds.Bottom - 1;

                e.Graphics.DrawLine(goldPen, e.Bounds.Left, top, e.Bounds.Right, top);
                e.Graphics.DrawLine(goldPen, e.Bounds.Left, bottom, e.Bounds.Right, bottom);

                if (e.ColumnIndex == 0)
                {
                    e.Graphics.DrawLine(goldPen, e.Bounds.Left, top, e.Bounds.Left, bottom);
                }
                if (e.ColumnIndex == 3)
                {
                    e.Graphics.DrawLine(goldPen, e.Bounds.Right - 1, top, e.Bounds.Right - 1, bottom);
                }
            }

            if (isSelected && (e.Item?.Focused ?? false))
            {
                e.DrawFocusRectangle(e.Bounds);
            }
        }

        private void NavigateSelected()
        {
            if (lstHistory.SelectedItems.Count > 0 && lstHistory.SelectedItems[0].Tag is VisitedPage page)
            {
                _onNavigate(page.Url);
                Close();
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            var result = CustomMessageBox.Show(this,
                TextManager.GetString("ClearHistoryConfirm"),
                TextManager.GetString("ClearHistory"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                IsDark);

            if (result == DialogResult.Yes)
            {
                _settingsManager.ClearHistory();
                LoadHistory();
            }
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                cmbCategoryFilter.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                cmbCategoryFilter.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lstHistory.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lstHistory.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                txtSelectedUrl.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                txtSelectedUrl.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                txtSelectedUrl.BorderStyle = BorderStyle.FixedSingle;
                btnNavigate.ForeColor = UIConstants.DarkMode.ThemeColor;
                btnNavigate.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
                btnClear.ForeColor = UIConstants.DarkMode.DangerColor;
                btnClear.FlatAppearance.BorderColor = UIConstants.DarkMode.DangerColor;
                btnClose.BackColor = UIConstants.DarkMode.ThemeColor;
                btnClose.ForeColor = Color.White;
            }
            else
            {
                BackColor = Color.White;
                lblTitle.ForeColor = Color.FromArgb(30, 30, 30);
                cmbCategoryFilter.BackColor = Color.White;
                cmbCategoryFilter.ForeColor = Color.FromArgb(30, 30, 30);
                lstHistory.BackColor = Color.White;
                lstHistory.ForeColor = Color.Black;
                txtSelectedUrl.BackColor = Color.White;
                txtSelectedUrl.ForeColor = Color.FromArgb(30, 30, 30);
                txtSelectedUrl.BorderStyle = BorderStyle.FixedSingle;
                btnNavigate.ForeColor = Color.FromArgb(74, 59, 42);
                btnNavigate.FlatAppearance.BorderColor = Color.FromArgb(180, 160, 140);
                btnClear.ForeColor = UIConstants.DangerColor;
                btnClear.FlatAppearance.BorderColor = UIConstants.DangerColor;
                btnClose.BackColor = UIConstants.ThemeColor;
                btnClose.ForeColor = Color.FromArgb(74, 59, 42);
            }

            lblSelectedBadge.UpdateTheme(IsDark);
        }
    }
}

