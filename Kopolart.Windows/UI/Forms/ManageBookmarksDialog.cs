using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Controls;
using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.Domain.Models;
using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Application.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Forms
{
    public class ManageBookmarksDialog : ThemedDialog
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IBookmarkImportExportService _importExportService;
        private readonly Action<string> _onNavigate;
        private List<BookmarkItem> _allBookmarks = new List<BookmarkItem>();

        private Label lblTitle = null!;
        private ComboBox cmbCategory = null!;
        private TextBox txtSearch = null!;
        private ListBox lstBookmarks = null!;
        private Label lblUrl = null!;
        private Button btnOpen = null!;
        private Button btnEdit = null!;
        private Button btnDelete = null!;
        private Button btnDeleteAll = null!;
        private Button btnExport = null!;
        private Button btnImport = null!;
        private Button btnClose = null!;

        public ManageBookmarksDialog(
            ISettingsManager settingsManager,
            bool isDark,
            Action<string> onNavigate,
            IBookmarkImportExportService? importExportService = null)
            : base(isDark)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _importExportService = importExportService ?? new BookmarkImportExportService();
            _onNavigate = onNavigate ?? throw new ArgumentNullException(nameof(onNavigate));

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            LoadBookmarks();
            InitializeTheme();
        }



        private void InitializeComponent()
        {
            lblTitle = new Label();
            cmbCategory = new ComboBox();
            txtSearch = new TextBox();
            lstBookmarks = new ListBox();
            lblUrl = new Label();
            btnOpen = new Button();
            btnEdit = new Button();
            btnDelete = new Button();
            btnDeleteAll = new Button();
            btnExport = new Button();
            btnImport = new Button();
            btnClose = new Button();

            SuspendLayout();

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(540, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = TextManager.GetString("BookmarksManagerTitle");

            lblTitle.Text = TextManager.GetString("BookmarksList");
            lblTitle.Font = FontManager.CreateBlackFont(12F);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(540 - 20 - lblTitle.PreferredWidth, 15);

            // Category Filter Dropdown
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            cmbCategory.Location = new Point(20, 45);
            cmbCategory.Size = new Size(500, 25);
            cmbCategory.Items.AddRange(new object[]
            {
                TextManager.GetString("CategoryAll"),
                TextManager.GetString("CategoryWallpaper"),
                TextManager.GetString("CategoryPost"),
                TextManager.GetString("CategoryColor"),
                TextManager.GetString("CategoryTools"),
                TextManager.GetString("CategoryOther")
            });
            cmbCategory.SelectedIndex = 0;
            cmbCategory.SelectedIndexChanged += (s, e) => FilterBookmarks();

            // Search TextBox with Placeholder Text
            string placeholder = TextManager.GetString("SearchBookmarks");
            txtSearch.Location = new Point(20, 80);
            txtSearch.Size = new Size(500, 23);
            txtSearch.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            txtSearch.PlaceholderText = placeholder;
            txtSearch.TextChanged += TxtSearch_TextChanged;

            // ListBox with OwnerDrawFixed to render Pill Badges and EndEllipsis ...
            lstBookmarks.Location = new Point(20, 115);
            lstBookmarks.Size = new Size(500, 220);
            lstBookmarks.Font = FontManager.CreateFont(9.5F, FontStyle.Regular);
            lstBookmarks.DrawMode = DrawMode.OwnerDrawFixed;
            lstBookmarks.ItemHeight = 36;
            lstBookmarks.DrawItem += LstBookmarks_DrawItem;
            lstBookmarks.SelectedIndexChanged += LstBookmarks_SelectedIndexChanged;
            lstBookmarks.DoubleClick += LstBookmarks_DoubleClick;

            // URL detail label
            lblUrl.Location = new Point(20, 345);
            lblUrl.Size = new Size(500, 45);
            lblUrl.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            lblUrl.Text = "";
            lblUrl.RightToLeft = RightToLeft.No;
            lblUrl.TextAlign = ContentAlignment.MiddleLeft;

            // Row 1 Buttons (RTL order: Open, Edit, Delete, DeleteAll)
            // Open button (Far right)
            btnOpen.Text = TextManager.GetString("BookmarkOpen");
            btnOpen.Size = new Size(115, 32);
            btnOpen.Location = new Point(540 - 20 - 115, 400);
            btnOpen.Cursor = Cursors.Hand;
            btnOpen.FlatStyle = FlatStyle.Flat;
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnOpen.Click += BtnOpen_Click;

            // Edit Title button
            btnEdit.Text = TextManager.GetString("EditBookmark");
            btnEdit.Size = new Size(115, 32);
            btnEdit.Location = new Point(280, 400);
            btnEdit.Cursor = Cursors.Hand;
            btnEdit.FlatStyle = FlatStyle.Flat;
            btnEdit.FlatAppearance.BorderSize = 1;
            btnEdit.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnEdit.Click += BtnEdit_Click;

            // Delete button
            btnDelete.Text = TextManager.GetString("DeleteBookmark");
            btnDelete.Size = new Size(115, 32);
            btnDelete.Location = new Point(155, 400);
            btnDelete.Cursor = Cursors.Hand;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 1;
            btnDelete.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnDelete.Click += BtnDelete_Click;

            // Delete All button (Far left)
            btnDeleteAll.Text = TextManager.GetString("DeleteAllBookmarks");
            btnDeleteAll.Size = new Size(125, 32);
            btnDeleteAll.Location = new Point(20, 400);
            btnDeleteAll.Cursor = Cursors.Hand;
            btnDeleteAll.FlatStyle = FlatStyle.Flat;
            btnDeleteAll.FlatAppearance.BorderSize = 1;
            btnDeleteAll.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnDeleteAll.Click += BtnDeleteAll_Click;

            // Row 2 Buttons (RTL order: Export, Import, Close)
            // Export button (Far right)
            btnExport.Text = TextManager.GetString("ExportBookmarks");
            btnExport.Size = new Size(115, 32);
            btnExport.Location = new Point(540 - 20 - 115, 445);
            btnExport.Cursor = Cursors.Hand;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 1;
            btnExport.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnExport.Click += BtnExport_Click;

            // Import button
            btnImport.Text = TextManager.GetString("ImportBookmarks");
            btnImport.Size = new Size(115, 32);
            btnImport.Location = new Point(280, 445);
            btnImport.Cursor = Cursors.Hand;
            btnImport.FlatStyle = FlatStyle.Flat;
            btnImport.FlatAppearance.BorderSize = 1;
            btnImport.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnImport.Click += BtnImport_Click;

            // Close button (Far left)
            btnClose.Text = TextManager.GetString("Close");
            btnClose.Size = new Size(125, 32);
            btnClose.Location = new Point(20, 445);
            btnClose.Cursor = Cursors.Hand;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = FontManager.CreateFont(9F, FontStyle.Regular);
            btnClose.Click += BtnClose_Click;

            Controls.Add(lblTitle);
            Controls.Add(cmbCategory);
            Controls.Add(txtSearch);
            Controls.Add(lstBookmarks);
            Controls.Add(lblUrl);
            Controls.Add(btnOpen);
            Controls.Add(btnEdit);
            Controls.Add(btnDelete);
            Controls.Add(btnDeleteAll);
            Controls.Add(btnExport);
            Controls.Add(btnImport);
            Controls.Add(btnClose);

            AcceptButton = btnOpen;
            CancelButton = btnClose;

            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadBookmarks()
        {
            _allBookmarks = _settingsManager.GetBookmarks() ?? new List<BookmarkItem>();
            FilterBookmarks();
        }

        private void FilterBookmarks()
        {
            lstBookmarks.BeginUpdate();

            string? selectedUrl = (lstBookmarks.SelectedItem is BookmarkItem selected) ? selected.Url : null;

            lstBookmarks.Items.Clear();
            string query = txtSearch.Text.Trim();
            int categoryIdx = cmbCategory.SelectedIndex;

            string targetCategory = categoryIdx switch
            {
                1 => "Wallpaper",
                2 => "Post",
                3 => "Color",
                4 => "Tools",
                5 => "Other",
                _ => "All"
            };

            foreach (var b in _allBookmarks)
            {
                if (targetCategory != "All" && !string.Equals(b.Category, targetCategory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(query) ||
                    b.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    b.Url.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    lstBookmarks.Items.Add(b);
                }
            }

            if (selectedUrl != null)
            {
                for (int i = 0; i < lstBookmarks.Items.Count; i++)
                {
                    if (lstBookmarks.Items[i] is BookmarkItem item && item.Url == selectedUrl)
                    {
                        lstBookmarks.SelectedIndex = i;
                        break;
                    }
                }
            }

            lstBookmarks.EndUpdate();
            UpdateUiState();
        }

        private void LstBookmarks_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstBookmarks.Items.Count)
                return;

            if (lstBookmarks.Items[e.Index] is not BookmarkItem item)
                return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Background color
            Color backColor;
            Color textColor;
            if (isSelected)
            {
                backColor = IsDark ? Color.FromArgb(70, 70, 75) : Color.FromArgb(220, 235, 252);
                textColor = IsDark ? Color.White : Color.Black;
            }
            else
            {
                backColor = IsDark ? UIConstants.DarkMode.CardBackgroundDark : Color.White;
                textColor = IsDark ? UIConstants.DarkMode.MenuStripForeColor : Color.FromArgb(30, 30, 30);
            }

            // Card bounds with luxury gold border
            Rectangle cardRect = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 2, e.Bounds.Width - 5, e.Bounds.Height - 4);

            using (var cardPath = CreateRoundedRectanglePath(cardRect, 4))
            using (var backBrush = new SolidBrush(backColor))
            using (var goldPen = new Pen(UIConstants.BrandGold, isSelected ? 2f : 1.2f))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(backBrush, cardPath);
                e.Graphics.DrawPath(goldPen, cardPath);
            }

            bool isRtl = true;

            // Resolve Localized Category Badge Name
            string badgeText = CategoryBadgeLabel.GetCategoryDisplayName(item.Category);
            var (badgeBgColor, badgeBorderColor, badgeTextColor) = CategoryBadgeLabel.GetCategoryColors(item.Category, IsDark);

            using var badgeFont = FontManager.CreateFont(8.5F, FontStyle.Bold);
            Size badgeSize = TextRenderer.MeasureText(e.Graphics, badgeText, badgeFont);
            int badgeHeight = Math.Max(22, badgeSize.Height + 4);
            int badgeWidth = badgeSize.Width + 12;

            int badgeY = e.Bounds.Y + (e.Bounds.Height - badgeHeight) / 2;
            int badgeX = isRtl
                ? e.Bounds.Right - badgeWidth - 8
                : e.Bounds.X + 8;

            Rectangle badgeRect = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);

            // Draw Pill Badge Background
            using (var badgePath = CreateRoundedRectanglePath(badgeRect, 5))
            using (var badgeBrush = new SolidBrush(badgeBgColor))
            using (var badgePen = new Pen(badgeBorderColor, 1))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(badgeBrush, badgePath);
                e.Graphics.DrawPath(badgePen, badgePath);
            }

            // Draw Badge Text
            TextFormatFlags badgeFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(e.Graphics, badgeText, badgeFont, badgeRect, badgeTextColor, badgeFlags);

            // Draw Title with EndEllipsis ...
            int titleX = isRtl
                ? e.Bounds.X + 8
                : badgeRect.Right + 10;
            int titleWidth = isRtl
                ? (badgeRect.Left - e.Bounds.X - 16)
                : (e.Bounds.Right - badgeRect.Right - 16);

            if (titleWidth > 10)
            {
                Rectangle titleRect = new Rectangle(titleX, e.Bounds.Y, titleWidth, e.Bounds.Height);
                TextFormatFlags titleFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                if (isRtl) titleFlags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;
                else titleFlags |= TextFormatFlags.Left;

                TextRenderer.DrawText(e.Graphics, item.Title, lstBookmarks.Font, titleRect, textColor, titleFlags);
            }

            e.DrawFocusRectangle();
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int cornerRadius)
        {
            var path = new GraphicsPath();
            int diameter = cornerRadius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            Rectangle arcRect = new Rectangle(rect.Location, new Size(diameter, diameter));

            // Top-left arc
            path.AddArc(arcRect, 180, 90);

            // Top-right arc
            arcRect.X = rect.Right - diameter;
            path.AddArc(arcRect, 270, 90);

            // Bottom-right arc
            arcRect.Y = rect.Bottom - diameter;
            path.AddArc(arcRect, 0, 90);

            // Bottom-left arc
            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);

            path.CloseFigure();
            return path;
        }

        private void UpdateUiState()
        {
            bool hasSelection = lstBookmarks.SelectedItem != null;
            btnOpen.Enabled = hasSelection;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;
            btnDeleteAll.Enabled = _allBookmarks.Count > 0;
            btnExport.Enabled = _allBookmarks.Count > 0;

            if (hasSelection && lstBookmarks.SelectedItem is BookmarkItem selected)
            {
                string localizedCat = CategoryBadgeLabel.GetCategoryDisplayName(selected.Category);
                lblUrl.Text = $"[{localizedCat}] {selected.Url}";
            }
            else
            {
                lblUrl.Text = "";
            }
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            FilterBookmarks();
        }

        private void ApplyLightTheme()
        {
            BackColor = Color.White;
            lblTitle.ForeColor = Color.FromArgb(30, 30, 30);
            lblUrl.ForeColor = Color.FromArgb(100, 100, 100);
            cmbCategory.BackColor = Color.White;
            cmbCategory.ForeColor = Color.Black;
            txtSearch.BackColor = Color.White;
            txtSearch.ForeColor = Color.FromArgb(30, 30, 30);
            txtSearch.BorderStyle = BorderStyle.Fixed3D;
            lstBookmarks.BackColor = Color.White;
            lstBookmarks.ForeColor = Color.FromArgb(30, 30, 30);
            lstBookmarks.BorderStyle = BorderStyle.Fixed3D;

            btnOpen.BackColor = UIConstants.ThemeColor;
            btnOpen.ForeColor = Color.FromArgb(74, 59, 42);

            btnEdit.BackColor = Color.Transparent;
            btnEdit.ForeColor = Color.FromArgb(74, 59, 42);
            btnEdit.FlatAppearance.BorderColor = Color.FromArgb(180, 160, 140);

            btnDelete.BackColor = Color.Transparent;
            btnDelete.ForeColor = UIConstants.DangerColor;
            btnDelete.FlatAppearance.BorderColor = UIConstants.DangerColor;

            btnDeleteAll.BackColor = Color.Transparent;
            btnDeleteAll.ForeColor = UIConstants.DangerColor;
            btnDeleteAll.FlatAppearance.BorderColor = UIConstants.DangerColor;

            btnExport.BackColor = Color.Transparent;
            btnExport.ForeColor = Color.FromArgb(74, 59, 42);
            btnExport.FlatAppearance.BorderColor = Color.FromArgb(180, 160, 140);

            btnImport.BackColor = Color.Transparent;
            btnImport.ForeColor = Color.FromArgb(74, 59, 42);
            btnImport.FlatAppearance.BorderColor = Color.FromArgb(180, 160, 140);

            btnClose.BackColor = Color.FromArgb(240, 240, 240);
            btnClose.ForeColor = Color.FromArgb(85, 85, 85);
        }

        private void ApplyDarkTheme()
        {
                BackColor = UIConstants.DarkMode.BrandBackground;
                lblTitle.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lblUrl.ForeColor = Color.FromArgb(180, 170, 160);
                cmbCategory.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                cmbCategory.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                txtSearch.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                txtSearch.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                txtSearch.BorderStyle = BorderStyle.FixedSingle;
                lstBookmarks.BackColor = UIConstants.DarkMode.CardBackgroundDark;
                lstBookmarks.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
                lstBookmarks.BorderStyle = BorderStyle.FixedSingle;

                btnOpen.BackColor = UIConstants.DarkMode.ThemeColor;
                btnOpen.ForeColor = Color.White;

                btnEdit.BackColor = Color.Transparent;
                btnEdit.ForeColor = UIConstants.DarkMode.ThemeColor;
                btnEdit.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

                btnDelete.BackColor = Color.Transparent;
                btnDelete.ForeColor = UIConstants.DarkMode.DangerColor;
                btnDelete.FlatAppearance.BorderColor = UIConstants.DarkMode.DangerColor;

                btnDeleteAll.BackColor = Color.Transparent;
                btnDeleteAll.ForeColor = UIConstants.DarkMode.DangerColor;
                btnDeleteAll.FlatAppearance.BorderColor = UIConstants.DarkMode.DangerColor;

                btnExport.BackColor = Color.Transparent;
                btnExport.ForeColor = UIConstants.DarkMode.ThemeColor;
                btnExport.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

                btnImport.BackColor = Color.Transparent;
                btnImport.ForeColor = UIConstants.DarkMode.ThemeColor;
                btnImport.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            btnClose.BackColor = Color.FromArgb(60, 60, 60);
            btnClose.ForeColor = UIConstants.DarkMode.MenuStripForeColor;
        }

        protected override void ApplyThemeColors(bool isDark)
        {
            if (isDark) ApplyDarkTheme();
            else ApplyLightTheme();
        }

        private void LstBookmarks_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateUiState();
        }

        private void LstBookmarks_DoubleClick(object? sender, EventArgs e)
        {
            BtnOpen_Click(sender, e);
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            if (lstBookmarks.SelectedItem is BookmarkItem selected)
            {
                _onNavigate(selected.Url);
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (lstBookmarks.SelectedItem is not BookmarkItem selected) return;

            string currentTitle = selected.Title;
            using var editForm = new BookmarkEditDialog(currentTitle, IsDark);

            if (editForm.ShowDialog(this) == DialogResult.OK)
            {
                var newTitle = editForm.BookmarkTitle;
                if (!string.IsNullOrEmpty(newTitle) && newTitle != currentTitle)
                {
                    _settingsManager.UpdateBookmarkTitle(selected.Url, newTitle);
                    selected.Title = newTitle;
                    FilterBookmarks();
                }
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (lstBookmarks.SelectedItem is BookmarkItem selected)
            {
                _settingsManager.RemoveBookmark(selected.Url);
                LoadBookmarks();
            }
        }

        private void BtnDeleteAll_Click(object? sender, EventArgs e)
        {
            var confirmResult = CustomMessageBox.Show(this,
                TextManager.GetString("DeleteAllBookmarksConfirm"),
                TextManager.GetString("DeleteAllBookmarks"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, IsDark);

            if (confirmResult == DialogResult.Yes)
            {
                _settingsManager.ClearAllBookmarks();
                LoadBookmarks();
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            var bookmarks = _settingsManager.GetBookmarks();
            if (bookmarks == null || bookmarks.Count == 0)
            {
                CustomMessageBox.Show(this, TextManager.GetString("NoBookmarksToExport"), TextManager.GetString("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning, IsDark);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = TextManager.GetString("ExportBookmarks"),
                Filter = "HTML Bookmark Files (*.html)|*.html|JSON Files (*.json)|*.json",
                FileName = $"kopolart_bookmarks_{DateTime.Now:yyyyMMdd}.html"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _importExportService.ExportToFile(sfd.FileName, bookmarks);
                    CustomMessageBox.Show(this, TextManager.GetString("BookmarksExported"), TextManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information, IsDark);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, ex.Message, TextManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
                }
            }
        }

        private void BtnImport_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = TextManager.GetString("ImportBookmarks"),
                Filter = "Bookmark Files (*.html;*.htm;*.json)|*.html;*.htm;*.json|HTML Files (*.html;*.htm)|*.html;*.htm|JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var items = _importExportService.ImportFromFile(ofd.FileName);
                    int importedCount = 0;

                    foreach (var item in items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Url) && !_settingsManager.IsBookmarked(item.Url))
                        {
                            _settingsManager.AddBookmark(item);
                            importedCount++;
                        }
                    }

                    if (importedCount > 0)
                    {
                        LoadBookmarks();
                        CustomMessageBox.Show(this, string.Format(TextManager.GetString("BookmarksImported"), importedCount), TextManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information, IsDark);
                    }
                    else
                    {
                        CustomMessageBox.Show(this, TextManager.GetString("ImportInvalidFile"), TextManager.GetString("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning, IsDark);
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, ex.Message, TextManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, IsDark);
                }
            }
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}

