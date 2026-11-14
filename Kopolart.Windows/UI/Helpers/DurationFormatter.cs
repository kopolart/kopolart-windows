using System;
using Kopolart.Windows.Domain.Helpers;

namespace Kopolart.Windows.UI.Helpers
{
    public static class DurationFormatter
    {
        public static string ToPersianDigits(string? input) => PersianNumberHelper.ToPersianDigits(input);

        /// <summary>
        /// Formats app usage duration in Persian:
        /// - Under 1 minute: "-- ثانیه"
        /// - Under 1 hour: "-- دقیقه و -- ثانیه" (or "-- دقیقه")
        /// - 1 hour or more (under 1 day): "-- ساعت و -- دقیقه" (or "-- ساعت")
        /// - 1 day or more with remaining hour at least 1: "-- روز و -- ساعت"
        /// - 1 day or more with remaining hour under 1: "-- روز و -- دقیقه" (or "-- روز")
        /// </summary>
        public static string FormatUsageDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            var totalDays = (int)duration.TotalDays;
            var totalHours = (int)duration.TotalHours;
            var totalMinutes = (int)duration.TotalMinutes;

            var seconds = duration.Seconds;
            var minutes = duration.Minutes;
            var hours = duration.Hours;

            // 1 day or more
            if (totalDays >= 1)
            {
                if (hours >= 1)
                {
                    return $"{ToPersianDigits(totalDays.ToString())} روز و {ToPersianDigits(hours.ToString())} ساعت";
                }

                // hour < 1
                if (minutes > 0)
                {
                    return $"{ToPersianDigits(totalDays.ToString())} روز و {ToPersianDigits(minutes.ToString())} دقیقه";
                }
                return $"{ToPersianDigits(totalDays.ToString())} روز";
            }

            // 1 hour or more -> Show hours and minutes only (no seconds)
            if (totalHours >= 1)
            {
                if (minutes > 0)
                {
                    return $"{ToPersianDigits(totalHours.ToString())} ساعت و {ToPersianDigits(minutes.ToString())} دقیقه";
                }
                return $"{ToPersianDigits(totalHours.ToString())} ساعت";
            }

            // Under 1 hour -> Show minutes and seconds
            if (totalMinutes >= 1)
            {
                if (seconds > 0)
                {
                    return $"{ToPersianDigits(totalMinutes.ToString())} دقیقه و {ToPersianDigits(seconds.ToString())} ثانیه";
                }
                return $"{ToPersianDigits(totalMinutes.ToString())} دقیقه";
            }

            // Under 1 minute -> Show seconds
            return $"{ToPersianDigits(seconds.ToString())} ثانیه";
        }

        /// <summary>
        /// Formats a TimeSpan duration into a user-friendly Persian string.
        /// </summary>
        public static string FormatDuration(TimeSpan elapsed)
        {
            if (elapsed.TotalMinutes < 1)
            {
                return TextManager.GetString("DurationFewSeconds");
            }
            if (elapsed.TotalHours < 1)
            {
                int minutes = (int)Math.Round(elapsed.TotalMinutes);
                string mStr = ToPersianDigits(minutes.ToString());
                return string.Format(TextManager.GetString("DurationMinutes"), mStr);
            }
            if (elapsed.TotalDays < 1)
            {
                int hours = (int)Math.Round(elapsed.TotalHours);
                string hStr = ToPersianDigits(hours.ToString());
                return string.Format(TextManager.GetString("DurationHours"), hStr);
            }
            int days = (int)Math.Round(elapsed.TotalDays);
            string dStr = ToPersianDigits(days.ToString());
            return string.Format(TextManager.GetString("DurationDays"), dStr);
        }
    }
}


