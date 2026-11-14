using Kopolart.Windows.Domain.Constants;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Provides direct access to Persian application string constants defined in AppConstants.
    /// All strings are compiled C# constants with zero runtime overhead.
    /// </summary>
    public static class TextManager
    {
        public static string GetString(string key)
        {
            return AppConstants.Strings.Get(key);
        }
    }
}
