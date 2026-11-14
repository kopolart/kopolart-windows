using System.Drawing;
using System.Threading.Tasks;

namespace Kopolart.Windows.Services
{
    /// <summary>
    /// UI-owned asset abstraction. Lives in the UI layer because it deals in GDI+
    /// types (Icon/Image) — keeping it out of the Application layer removes that
    /// layer's System.Drawing dependency.
    /// </summary>
    public interface IAssetService
    {
        Icon? GetAppIcon();
        Image? LoadLogo(bool isDark = false);
        Image? LoadLogo(string? filename);
        Task<Image?> LoadLogoAsync(bool isDark = false);
        Task<Image?> LoadLogoAsync(string? filename);
    }
}
