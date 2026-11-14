using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kopolart.Windows.Domain.Helpers;

namespace Kopolart.Windows.Application.Services
{
    public class UsageKpiSummary
    {
        public long TodayUsageMs { get; set; }
        public long PeriodTotalMs { get; set; }
        public long AverageMs { get; set; }
        public long PeakMs { get; set; }
        public string PeakLabel { get; set; } = "-";
        public long TodayInternetBytes { get; set; }
        public long PeriodInternetBytes { get; set; }
        public long LifetimeInternetBytes { get; set; }
        public string FormattedTodayUsage { get; set; } = "";
        public string FormattedAverageUsage { get; set; } = "";
        public string FormattedTotalUsage { get; set; } = "";
        public string FormattedPeriodData { get; set; } = "";
        public string FormattedTodayData { get; set; } = "";
        public string FormattedLifetimeData { get; set; } = "";
        public string TooltipText { get; set; } = "";
    }

    public static class UsageAnalyticsService
    {
        public static UsageKpiSummary CalculateKpiSummary(
            Dictionary<string, long> dailyUsage,
            Dictionary<string, long>? dailyInternet,
            long totalLifetimeInternetBytes,
            long liveTodayInternetBytes,
            int daysRange,
            bool isMonthlyMode,
            long monthlyPeriodTotalMs,
            long monthlyAverageMs,
            long monthlyPeakMs,
            string? monthlyPeakName)
        {
            var summary = new UsageKpiSummary();
            var today = DateTime.Today;
            string todayKey = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // 1. Today Usage
            dailyUsage.TryGetValue(todayKey, out var todayMs);
            summary.TodayUsageMs = todayMs;
            summary.FormattedTodayUsage = FormatDurationCompact(todayMs);

            // 2. Period Average, Total & Peak
            if (isMonthlyMode)
            {
                summary.PeriodTotalMs = monthlyPeriodTotalMs;
                summary.AverageMs = monthlyAverageMs;
                summary.PeakMs = monthlyPeakMs;
                summary.FormattedAverageUsage = FormatDurationCompact(monthlyAverageMs);
                summary.FormattedTotalUsage = FormatDurationCompact(monthlyPeriodTotalMs);

                if (monthlyPeakMs > 0 && !string.IsNullOrWhiteSpace(monthlyPeakName))
                {
                    summary.PeakLabel = $"{monthlyPeakName}\n{FormatDurationCompact(monthlyPeakMs)}";
                }
                else
                {
                    summary.PeakLabel = "-";
                }
            }
            else
            {
                long periodTotalMs = 0;
                long peakMs = 0;
                DateTime? peakDate = null;

                for (int i = daysRange - 1; i >= 0; i--)
                {
                    var dt = today.AddDays(-i);
                    var key = dt.ToString("yyyy-MM-dd");
                    dailyUsage.TryGetValue(key, out var ms);

                    periodTotalMs += ms;
                    if (ms > peakMs)
                    {
                        peakMs = ms;
                        peakDate = dt;
                    }
                }

                long avgMs = daysRange > 0 ? periodTotalMs / daysRange : 0;
                summary.PeriodTotalMs = periodTotalMs;
                summary.AverageMs = avgMs;
                summary.PeakMs = peakMs;
                summary.FormattedAverageUsage = FormatDurationCompact(avgMs);
                summary.FormattedTotalUsage = FormatDurationCompact(periodTotalMs);

                if (peakMs > 0 && peakDate.HasValue)
                {
                    summary.PeakLabel = $"{GetPersianDayOfWeekName(peakDate.Value.DayOfWeek)}\n{FormatDurationCompact(peakMs)}";
                }
                else
                {
                    summary.PeakLabel = "-";
                }
            }

            // 3. Internet Data
            if (dailyInternet != null && dailyInternet.Count > 0)
            {
                long sumAll = dailyInternet.Values.Sum();
                if (sumAll > totalLifetimeInternetBytes)
                {
                    totalLifetimeInternetBytes = sumAll;
                }
            }
            summary.LifetimeInternetBytes = totalLifetimeInternetBytes;

            long todayDataBytes = liveTodayInternetBytes > 0
                ? liveTodayInternetBytes
                : (dailyInternet != null && dailyInternet.TryGetValue(todayKey, out var tb) ? tb : 0L);
            summary.TodayInternetBytes = todayDataBytes;

            long periodDataBytes = 0;
            if (daysRange >= 365)
            {
                periodDataBytes = totalLifetimeInternetBytes;
            }
            else if (dailyInternet != null)
            {
                for (int i = daysRange - 1; i >= 0; i--)
                {
                    var dt = today.AddDays(-i);
                    var key = dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (dailyInternet.TryGetValue(key, out var b))
                    {
                        periodDataBytes += b;
                    }
                }
            }
            summary.PeriodInternetBytes = periodDataBytes;

            summary.FormattedTodayData = UsageTrackerService.FormatDataSize(todayDataBytes);
            summary.FormattedPeriodData = UsageTrackerService.FormatDataSize(periodDataBytes);
            summary.FormattedLifetimeData = UsageTrackerService.FormatDataSize(totalLifetimeInternetBytes);

            summary.TooltipText = $"امروز: {summary.FormattedTodayData}\nمجموع دوره: {summary.FormattedPeriodData}\nکل حجم اینترنت مصرفی: {summary.FormattedLifetimeData}";

            return summary;
        }

        public static string GetPersianDayOfWeekName(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Saturday => "شنبه",
            DayOfWeek.Sunday => "یکشنبه",
            DayOfWeek.Monday => "دوشنبه",
            DayOfWeek.Tuesday => "سه‌شنبه",
            DayOfWeek.Wednesday => "چهارشنبه",
            DayOfWeek.Thursday => "پنج‌شنبه",
            DayOfWeek.Friday => "جمعه",
            _ => ""
        };

        public static string ToPersianDigits(string input) => PersianNumberHelper.ToPersianDigits(input);

        public static string FormatDurationCompact(long ms)
        {
            if (ms <= 0) return "۰ ثانیه";
            var ts = TimeSpan.FromMilliseconds(ms);
            int d = (int)ts.TotalDays;
            int h = ts.Hours;
            int totalH = (int)ts.TotalHours;
            int m = ts.Minutes;
            int s = ts.Seconds;

            string res;
            if (d >= 1)
            {
                res = h > 0 ? $"{d} روز و {h} ساعت" : $"{d} روز";
            }
            else if (totalH >= 1)
            {
                res = m > 0 ? $"{totalH} ساعت و {m} دقیقه" : $"{totalH} ساعت";
            }
            else if (m > 0)
            {
                res = s > 0 ? $"{m} دقیقه و {s} ثانیه" : $"{m} دقیقه";
            }
            else
            {
                res = $"{s} ثانیه";
            }

            return ToPersianDigits(res);
        }
    }
}
