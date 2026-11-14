using System.Collections.Generic;
using Kopolart.Windows.Domain.Models;

namespace Kopolart.Windows.Application.Interfaces
{
    public interface IWallpaperService
    {
        IReadOnlyList<WallpaperMonitorInfo> GetMonitors();

        WallpaperSetResult SetWallpaper(string imagePath, string? monitorId, WallpaperPlacement placement);
    }
}
