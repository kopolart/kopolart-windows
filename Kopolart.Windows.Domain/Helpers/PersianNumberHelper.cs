using System.Text;

namespace Kopolart.Windows.Domain.Helpers
{
    /// <summary>
    /// High-performance utility for converting ASCII numbers into Persian/Farsi digits.
    /// Centralized in Domain layer for reuse across Domain, Application, Infrastructure, and UI layers.
    /// </summary>
    public static class PersianNumberHelper
    {
        private static readonly char[] PersianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];

        /// <summary>
        /// Converts all ASCII digits ('0'-'9') in the input string to Persian digits ('۰'-'۹').
        /// </summary>
        public static string ToPersianDigits(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }

            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (ch >= '0' && ch <= '9')
                {
                    sb.Append(PersianDigits[ch - '0']);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts an integer to a Persian-digit string representation.
        /// </summary>
        public static string ToPersianDigits(int value)
        {
            return ToPersianDigits(value.ToString());
        }

        /// <summary>
        /// Converts a long integer to a Persian-digit string representation.
        /// </summary>
        public static string ToPersianDigits(long value)
        {
            return ToPersianDigits(value.ToString());
        }
    }
}
