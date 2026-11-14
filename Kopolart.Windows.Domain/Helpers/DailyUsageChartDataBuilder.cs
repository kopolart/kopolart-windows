using Kopolart.Windows.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Kopolart.Windows.Domain.Helpers
{
    public class DailyChartDataSet
    {
        public List<DailyChartBarData> Items { get; set; } = new();
        public long MaxMilliseconds { get; set; }
        public long TotalMilliseconds { get; set; }
        public double AverageMilliseconds { get; set; }
        public bool IsMonthlyMode { get; set; }
        public DailyChartBarData? PeakItem => Items.Where(x => x.Milliseconds > 0).OrderByDescending(x => x.Milliseconds).FirstOrDefault();
    }

    public static class DailyUsageChartDataBuilder
    {
        public static DailyChartDataSet BuildDataSet(Dictionary<string, long> dailyUsage, int daysCount)
        {
            var dataSet = new DailyChartDataSet();
            var today = DateTime.Today;
            var persianCal = new PersianCalendar();

            long maxVal = 0;
            long totalVal = 0;
            var list = new List<DailyChartBarData>();

            if (daysCount >= 365)
            {
                dataSet.IsMonthlyMode = true;

                int curPYear = persianCal.GetYear(today);
                int curPMonth = persianCal.GetMonth(today);

                var monthSums = new Dictionary<(int Year, int Month), long>();
                var monthMeta = new List<(int Year, int Month)>();

                for (int m = 0; m < 12; m++)
                {
                    int offset = 11 - m;
                    int totalMonthIndex = (curPYear * 12 + (curPMonth - 1)) - offset;
                    int targetYear = totalMonthIndex / 12;
                    int targetMonth = (totalMonthIndex % 12) + 1;

                    monthSums[(targetYear, targetMonth)] = 0L;
                    monthMeta.Add((targetYear, targetMonth));
                }

                foreach (var kvp in dailyUsage)
                {
                    if (DateTime.TryParseExact(kvp.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        try
                        {
                            int py = persianCal.GetYear(dt);
                            int pm = persianCal.GetMonth(dt);
                            if (monthSums.ContainsKey((py, pm)))
                            {
                                monthSums[(py, pm)] += kvp.Value;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }

                foreach (var (year, month) in monthMeta)
                {
                    long ms = monthSums[(year, month)];
                    if (ms > maxVal) maxVal = ms;
                    totalVal += ms;

                    string monthName = GetPersianMonthName(month);
                    string yearStr = ToPersianDigits(year.ToString());
                    bool isCurrent = (year == curPYear && month == curPMonth);

                    DateTime monthDate;
                    try
                    {
                        monthDate = persianCal.ToDateTime(year, month, 1, 0, 0, 0, 0);
                    }
                    catch
                    {
                        monthDate = today;
                    }

                    list.Add(new DailyChartBarData
                    {
                        Date = monthDate,
                        DateKey = $"{year:D4}-{month:D2}",
                        Milliseconds = ms,
                        DayName = monthName,
                        DayNumber = yearStr,
                        DateLabel = $"{monthName} {yearStr}",
                        FullDateString = $"{monthName} {yearStr}",
                        FormattedDuration = FormatDuration(ms),
                        IsCurrentPeriod = isCurrent
                    });
                }

                dataSet.TotalMilliseconds = totalVal;
                dataSet.AverageMilliseconds = totalVal / 12.0;
                dataSet.MaxMilliseconds = Math.Max(maxVal, 60 * 1000);
            }
            else
            {
                dataSet.IsMonthlyMode = false;

                for (int i = daysCount - 1; i >= 0; i--)
                {
                    var dt = today.AddDays(-i);
                    var key = dt.ToString("yyyy-MM-dd");
                    dailyUsage.TryGetValue(key, out var ms);

                    if (ms > maxVal) maxVal = ms;
                    totalVal += ms;

                    string dayName = GetPersianDayOfWeekName(dt.DayOfWeek);
                    int pMonth = persianCal.GetMonth(dt);
                    int pDay = persianCal.GetDayOfMonth(dt);
                    string dayNumber = ToPersianDigits(pDay.ToString());
                    string dateLabel = $"{dayNumber} {GetPersianMonthName(pMonth)}";
                    string fullDateStr = $"{dayName}، {dayNumber} {GetPersianMonthName(pMonth)} {ToPersianDigits(persianCal.GetYear(dt).ToString())}";

                    list.Add(new DailyChartBarData
                    {
                        Date = dt,
                        DateKey = key,
                        Milliseconds = ms,
                        DayName = dayName,
                        DayNumber = dayNumber,
                        DateLabel = dateLabel,
                        FullDateString = fullDateStr,
                        FormattedDuration = FormatDuration(ms),
                        IsCurrentPeriod = (dt.Date == today.Date)
                    });
                }

                dataSet.TotalMilliseconds = totalVal;
                dataSet.AverageMilliseconds = daysCount > 0 ? (double)totalVal / daysCount : 0;
                dataSet.MaxMilliseconds = Math.Max(maxVal, 60 * 1000);
            }

            foreach (var item in list)
            {
                item.PercentageOfTotal = dataSet.TotalMilliseconds > 0
                    ? ((double)item.Milliseconds / dataSet.TotalMilliseconds) * 100.0
                    : 0.0;
            }

            dataSet.Items = list;
            return dataSet;
        }

        public static string GetPersianMonthName(int month) => month switch
        {
            1 => "فروردین",
            2 => "اردیبهشت",
            3 => "خرداد",
            4 => "تیر",
            5 => "مرداد",
            6 => "شهریور",
            7 => "مهر",
            8 => "آبان",
            9 => "آذر",
            10 => "دی",
            11 => "بهمن",
            12 => "اسفند",
            _ => ""
        };

        public static string GetPersianDayOfWeekName(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Saturday => "شنبه",
            DayOfWeek.Sunday => "یکشنبه",
            DayOfWeek.Monday => "دوشنبه",
            DayOfWeek.Tuesday => "سه‌شنبه",
            DayOfWeek.Wednesday => "چهارشنبه",
            DayOfWeek.Thursday => "پنجشنبه",
            DayOfWeek.Friday => "جمعه",
            _ => ""
        };

        public static string ToPersianDigits(string input) => PersianNumberHelper.ToPersianDigits(input);

        public static string FormatDuration(long ms)
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
                if (h >= 1)
                {
                    res = $"{d} روز و {h} ساعت";
                }
                else if (m > 0)
                {
                    res = $"{d} روز و {m} دقیقه";
                }
                else
                {
                    res = $"{d} روز";
                }
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
