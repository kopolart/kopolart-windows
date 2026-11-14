using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Kopolart.Windows.Services
{
    public class AssetService : IAssetService
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public Icon? GetAppIcon()
        {
            try
            {
                var bitmap = Properties.Resources.kopolart_icon;
                if (bitmap == null)
                {
                    return null;
                }

                var hIcon = bitmap.GetHicon();
                try
                {
                    return (Icon)Icon.FromHandle(hIcon).Clone();
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Loads the app logo based on theme (light or dark).
        /// </summary>
        public Image? LoadLogo(bool isDark = false)
        {
            try
            {
                return isDark
                    ? Properties.Resources.kopolart_logo_dark
                    : Properties.Resources.kopolart_logo;
            }
            catch
            {
                return null;
            }
        }

        public Image? LoadLogo(string? filename)
        {
            if (!string.IsNullOrEmpty(filename) &&
                filename.Contains("dark", StringComparison.OrdinalIgnoreCase))
            {
                return LoadLogo(isDark: true);
            }

            return LoadLogo(isDark: false);
        }

        public Task<Image?> LoadLogoAsync(bool isDark = false)
        {
            return Task.FromResult(LoadLogo(isDark));
        }

        public Task<Image?> LoadLogoAsync(string? filename)
        {
            return Task.FromResult(LoadLogo(filename));
        }
    }
}
