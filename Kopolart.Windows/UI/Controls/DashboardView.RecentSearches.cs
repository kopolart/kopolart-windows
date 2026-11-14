using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Controls
{
    public partial class DashboardView : UserControl
    {

        private void CreateRecentSearchChips()
        {
            if (pnlRecentSearches == null) return;

            foreach (var chip in _recentSearchChips)
            {
                pnlRecentSearches.Controls.Remove(chip);
                chip.Dispose();
            }
            _recentSearchChips.Clear();

            var recents = _settingsManager.GetRecentSearches();
            if (recents == null || recents.Count == 0)
            {
                pnlRecentSearches.Visible = false;
                pnlRecentSearches.Height = 0;
                return;
            }

            pnlRecentSearches.Visible = true;
            pnlRecentSearches.Height = 28;
            pnlRecentSearches.BringToFront();

            foreach (var query in recents.Take(5))
            {
                var chip = new DashboardRecentSearchChip(query, _isDark);
                chip.ChipClicked += (s, q) =>
                {
                    searchBar.SetSearchQuery(q, execute: true);
                };
                chip.DeleteClicked += (s, q) =>
                {
                    _settingsManager.RemoveRecentSearch(q);
                    RefreshRecentSearches();
                };
                _recentSearchChips.Add(chip);
                pnlRecentSearches.Controls.Add(chip);
            }

            LayoutRecentSearches();
        }

        public void RefreshRecentSearches()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshRecentSearches));
                return;
            }
            CreateRecentSearchChips();
            CenterContentPanel();
        }

        private void LayoutRecentSearches()
        {
            if (pnlRecentSearches == null) return;
            if (_recentSearchChips.Count == 0)
            {
                pnlRecentSearches.Visible = false;
                pnlRecentSearches.Height = 0;
                return;
            }

            pnlRecentSearches.Visible = true;
            pnlRecentSearches.Height = 28;

            int containerW = pnlRecentSearches.ClientSize.Width;
            if (containerW <= 0) containerW = pnlRecentSearches.Width;
            if (containerW <= 0) return;

            int padRight = 4;
            int startX = containerW - padRight;

            // In RTL: header icon is at the far right
            lblRecentIcon.Location = new Point(startX - lblRecentIcon.Width, (pnlRecentSearches.Height - lblRecentIcon.Height) / 2);

            // header text is directly to the left of the icon
            lblRecentText.Location = new Point(lblRecentIcon.Left - lblRecentText.Width - 3, (pnlRecentSearches.Height - lblRecentText.Height) / 2);

            int curX = lblRecentText.Left - 8;

            foreach (var chip in _recentSearchChips)
            {
                int chipX = curX - chip.Width;
                if (chipX < 0)
                {
                    chip.Visible = false;
                }
                else
                {
                    chip.Visible = true;
                    chip.Size = new Size(chip.Width, 28);
                    chip.Location = new Point(chipX, (pnlRecentSearches.Height - chip.Height) / 2);
                    curX -= (chip.Width + 6);
                }
            }
        }

    }
}
