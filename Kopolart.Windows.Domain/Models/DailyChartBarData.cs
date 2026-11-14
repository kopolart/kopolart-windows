using System;
using System.Drawing;

namespace Kopolart.Windows.Domain.Models
{
    public class DailyChartBarData
    {
        public DateTime Date { get; set; }
        public string DateKey { get; set; } = "";
        public long Milliseconds { get; set; }
        public string DayName { get; set; } = "";
        public string DayNumber { get; set; } = "";
        public string DateLabel { get; set; } = "";
        public string FullDateString { get; set; } = "";
        public string FormattedDuration { get; set; } = "";
        public double PercentageOfTotal { get; set; }
        public RectangleF BarRect { get; set; }
        public RectangleF ColumnHitArea { get; set; }
        public bool IsCurrentPeriod { get; set; }
    }
}
