namespace Kopolart.Windows.Domain.Models
{
    public class WallpaperMonitorInfo
    {
        public string Id { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsPrimary { get; set; }

        public double AspectRatio => Width > 0 && Height > 0 ? (double)Width / Height : 16.0 / 9.0;
    }
}
