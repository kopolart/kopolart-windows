using System;
using System.Globalization;

namespace Kopolart.Windows.Domain.Helpers
{
    /// <summary>
    /// Utility for converting dates to Persian Solar Hijri (Shamsi) representation.
    /// Resides in Domain layer to be reused across all application layers.
    /// </summary>
    public static class PersianDateHelper
    {
        private static readonly string[] PersianMonthNames =
        [
            "فروردین", "اردیبهشت", "خرداد",
            "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر",
            "دی", "بهمن", "اسفند"
        ];

        /// <summary>
        /// Converts a date string (e.g. "09/26/2026", "2026-09-26", "2026/09/26") or DateTime
        /// into Persian Solar Hijri date with Persian month name (e.g. "۵ مهر ۱۴۰۵").
        /// </summary>
        public static string ToPersianDateString(string? rawDate, bool includeMonthName = true)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
                return string.Empty;

            string[] formats = ["MM/dd/yyyy", "M/d/yyyy", "yyyy/MM/dd", "yyyy-MM-dd", "yyyy.MM.dd", "O", "s"];
            if (DateTime.TryParseExact(rawDate.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                DateTime.TryParse(rawDate.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                return ToPersianDateString(dt, includeMonthName);
            }

            return PersianNumberHelper.ToPersianDigits(rawDate);
        }

        /// <summary>
        /// Converts a DateTime into Persian Solar Hijri date with Persian month name (e.g. "۵ مهر ۱۴۰۵").
        /// </summary>
        public static string ToPersianDateString(DateTime date, bool includeMonthName = true)
        {
            try
            {
                var pc = new PersianCalendar();
                int year = pc.GetYear(date);
                int month = pc.GetMonth(date);
                int day = pc.GetDayOfMonth(date);

                if (includeMonthName && month >= 1 && month <= 12)
                {
                    return $"{PersianNumberHelper.ToPersianDigits(day)} {PersianMonthNames[month - 1]} {PersianNumberHelper.ToPersianDigits(year)}";
                }

                return PersianNumberHelper.ToPersianDigits($"{year:0000}/{month:00}/{day:00}");
            }
            catch
            {
                return PersianNumberHelper.ToPersianDigits(date.ToString("yyyy/MM/dd"));
            }
        }

        /// <summary>
        /// Maps standard DayOfWeek to Persian weekday name.
        /// </summary>
        public static string GetPersianDayOfWeekName(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Saturday => "شنبه",
            DayOfWeek.Sunday => "یکشنبه",
            DayOfWeek.Monday => "دوشنبه",
            DayOfWeek.Tuesday => "سه‌شنبه",
            DayOfWeek.Wednesday => "چهارشنبه",
            DayOfWeek.Thursday => "پنج‌شنبه",
            DayOfWeek.Friday => "جمعه",
            _ => string.Empty
        };

        /// <summary>
        /// Returns today's Persian date in format: "امروز {روز هفته} {روز} {ماه}"
        /// Example: "امروز دوشنبه ۲۲ فروردین"
        /// </summary>
        public static string GetPersianTodayDateString(DateTime? localDate = null)
        {
            try
            {
                var dt = localDate ?? DateTime.Now;
                var pc = new PersianCalendar();
                int month = pc.GetMonth(dt);
                int day = pc.GetDayOfMonth(dt);
                string dayOfWeek = GetPersianDayOfWeekName(dt.DayOfWeek);
                string monthName = (month >= 1 && month <= 12) ? PersianMonthNames[month - 1] : string.Empty;

                return $"امروز {dayOfWeek} {PersianNumberHelper.ToPersianDigits(day)} {monthName}".Trim();
            }
            catch
            {
                return "امروز";
            }
        }
    }
}
