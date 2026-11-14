using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Microsoft.Win32;

namespace Kopolart.Windows.Infrastructure.Services
{
    /// <summary>
    /// Applies desktop wallpapers through the Windows shell (IDesktopWallpaper, Windows 8+)
    /// and falls back to SystemParametersInfo when the COM API is unavailable.
    /// </summary>
    public sealed class WindowsWallpaperService : IWallpaperService
    {
        private const uint SpiSetDeskWallpaper = 0x0014;
        private const uint SpifUpdateIniFile = 0x0001;
        private const uint SpifSendChange = 0x0002;
        private const int SmCxScreen = 0;
        private const int SmCyScreen = 1;
        private const long JpegQuality = 92;
        private const string DesktopRegistryKey = @"Control Panel\Desktop";

        private static readonly string[] NativeFormats = [".jpg", ".jpeg", ".png", ".bmp"];

        public IReadOnlyList<WallpaperMonitorInfo> GetMonitors()
        {
            var monitors = ReadMonitors();
            return monitors.Count > 0 ? monitors : CreateFallbackMonitors();
        }

        public WallpaperSetResult SetWallpaper(string imagePath, string? monitorId, WallpaperPlacement placement)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                return WallpaperSetResult.Fail(WallpaperSetStatus.FileMissing);

            if (!WallpaperEligibility.IsSupportedImage(imagePath))
                return WallpaperSetResult.Fail(WallpaperSetStatus.UnsupportedFormat);

            string preparedPath;
            try
            {
                preparedPath = PrepareImage(imagePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsWallpaperService] Image conversion failed: {ex.Message}");
                return WallpaperSetResult.Fail(WallpaperSetStatus.ConversionFailed, ex.Message);
            }

            return Apply(preparedPath, monitorId, placement)
                ? WallpaperSetResult.Success()
                : WallpaperSetResult.Fail(WallpaperSetStatus.Failed);
        }

        private static List<WallpaperMonitorInfo> ReadMonitors()
        {
            var monitors = new List<WallpaperMonitorInfo>();
            try
            {
                var desktop = (IDesktopWallpaper)new DesktopWallpaper();
                desktop.GetMonitorDevicePathCount(out uint count);

                for (uint index = 0; index < count; index++)
                {
                    desktop.GetMonitorDevicePathAt(index, out string deviceId);
                    if (string.IsNullOrWhiteSpace(deviceId))
                        continue;

                    desktop.GetMonitorRECT(deviceId, out NativeRect rect);
                    monitors.Add(new WallpaperMonitorInfo
                    {
                        Id = deviceId,
                        Width = Math.Max(0, rect.Right - rect.Left),
                        Height = Math.Max(0, rect.Bottom - rect.Top),
                        IsPrimary = rect.Left == 0 && rect.Top == 0
                    });
                }

                if (monitors.Count > 0 && !monitors.Any(m => m.IsPrimary))
                    monitors[0].IsPrimary = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsWallpaperService] Monitor enumeration failed: {ex.Message}");
                monitors.Clear();
            }

            return monitors;
        }

        private static List<WallpaperMonitorInfo> CreateFallbackMonitors()
        {
            return
            [
                new WallpaperMonitorInfo
                {
                    Id = "",
                    Width = Math.Max(0, GetSystemMetrics(SmCxScreen)),
                    Height = Math.Max(0, GetSystemMetrics(SmCyScreen)),
                    IsPrimary = true
                }
            ];
        }

        private static string PrepareImage(string imagePath)
        {
            string extension = Path.GetExtension(imagePath).ToLowerInvariant();
            if (NativeFormats.Contains(extension))
                return imagePath;

            string cacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Kopolart",
                "Wallpapers");
            Directory.CreateDirectory(cacheFolder);

            string targetPath = Path.Combine(cacheFolder, BuildCacheFileName(imagePath));
            if (File.Exists(targetPath))
                return targetPath;

            string temporaryPath = targetPath + ".tmp";
            using (var source = Image.FromFile(imagePath))
            using (var bitmap = new Bitmap(source.Width, source.Height))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height));
                }

                var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid)
                    ?? throw new InvalidOperationException("JPEG encoder is not available.");

                using var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JpegQuality);
                bitmap.Save(temporaryPath, jpegEncoder, encoderParameters);
            }

            File.Move(temporaryPath, targetPath, overwrite: true);
            return targetPath;
        }

        private static string BuildCacheFileName(string imagePath)
        {
            var fileInfo = new FileInfo(imagePath);
            string key = $"{imagePath}|{fileInfo.LastWriteTimeUtc.Ticks}|{fileInfo.Length}";
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            return $"wallpaper_{Convert.ToHexString(hash, 0, 12).ToLowerInvariant()}.jpg";
        }

        private static bool Apply(string imagePath, string? monitorId, WallpaperPlacement placement)
        {
            try
            {
                var desktop = (IDesktopWallpaper)new DesktopWallpaper();
                desktop.SetWallpaper(string.IsNullOrWhiteSpace(monitorId) ? null : monitorId, imagePath);
                desktop.SetPosition(ToNativePosition(placement));
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsWallpaperService] IDesktopWallpaper failed: {ex.Message}");
            }

            try
            {
                ApplyPlacementToRegistry(placement);
                return SystemParametersInfo(SpiSetDeskWallpaper, 0, imagePath, SpifUpdateIniFile | SpifSendChange);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsWallpaperService] SystemParametersInfo failed: {ex.Message}");
                return false;
            }
        }

        private static void ApplyPlacementToRegistry(WallpaperPlacement placement)
        {
            using var key = Registry.CurrentUser.OpenSubKey(DesktopRegistryKey, writable: true);
            if (key == null)
                return;

            (string style, string tile) = placement switch
            {
                WallpaperPlacement.Fill => ("10", "0"),
                WallpaperPlacement.Fit => ("6", "0"),
                WallpaperPlacement.Stretch => ("2", "0"),
                WallpaperPlacement.Center => ("0", "0"),
                WallpaperPlacement.Tile => ("0", "1"),
                WallpaperPlacement.Span => ("22", "0"),
                _ => ("10", "0")
            };

            key.SetValue("WallpaperStyle", style, RegistryValueKind.String);
            key.SetValue("TileWallpaper", tile, RegistryValueKind.String);
        }

        private static DesktopWallpaperPosition ToNativePosition(WallpaperPlacement placement)
        {
            return placement switch
            {
                WallpaperPlacement.Fill => DesktopWallpaperPosition.Fill,
                WallpaperPlacement.Fit => DesktopWallpaperPosition.Fit,
                WallpaperPlacement.Stretch => DesktopWallpaperPosition.Stretch,
                WallpaperPlacement.Center => DesktopWallpaperPosition.Center,
                WallpaperPlacement.Tile => DesktopWallpaperPosition.Tile,
                WallpaperPlacement.Span => DesktopWallpaperPosition.Span,
                _ => DesktopWallpaperPosition.Fill
            };
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private enum DesktopWallpaperPosition
        {
            Center = 0,
            Tile = 1,
            Stretch = 2,
            Span = 3,
            Fill = 4,
            Fit = 5
        }

        private enum DesktopSlideshowOptions
        {
            Shuffle = 0,
            StopOnBattery = 1
        }

        private enum DesktopSlideshowDirection
        {
            Forward = 0,
            Backward = 1
        }

        [Flags]
        private enum DesktopSlideshowState
        {
            Enabled = 1,
            Slideshow = 2,
            DisabledByRemoteSession = 4
        }

        [ComImport]
        [Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")]
        private class DesktopWallpaper
        {
        }

        // Methods must stay in vtable order; only the first nine are used.
        [ComImport]
        [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDesktopWallpaper
        {
            void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
            void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorId, [MarshalAs(UnmanagedType.LPWStr)] out string wallpaper);
            void GetMonitorDevicePathAt(uint monitorIndex, [MarshalAs(UnmanagedType.LPWStr)] out string monitorId);
            void GetMonitorDevicePathCount(out uint count);
            void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId, out NativeRect displayRect);
            void SetBackgroundColor(uint color);
            void GetBackgroundColor(out uint color);
            void SetPosition(DesktopWallpaperPosition position);
            void GetPosition(out DesktopWallpaperPosition position);
            void SetSlideshow(IntPtr items);
            void GetSlideshow(out IntPtr items);
            void SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick);
            void GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick);
            void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, DesktopSlideshowDirection direction);
            void GetStatus(out DesktopSlideshowState state);
            void Enable(bool enable);
        }
    }
}
