using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Kopolart.Windows.Domain.Models;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Renders the desktop preview for a wallpaper using the same placement semantics
    /// Windows applies to the real desktop.
    /// </summary>
    public static class WallpaperPreviewRenderer
    {
        public static Bitmap Render(Image source, Size canvas, double targetAspect, WallpaperPlacement placement)
        {
            int width = Math.Max(1, canvas.Width);
            int height = Math.Max(1, canvas.Height);

            var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Black);

            Rectangle frame = FitAspect(width, height, targetAspect);
            GraphicsState state = graphics.Save();
            graphics.SetClip(frame);
            DrawPlacement(graphics, source, frame, placement);
            graphics.Restore(state);

            using var borderPen = new Pen(Color.FromArgb(55, 55, 55));
            graphics.DrawRectangle(borderPen, frame.X, frame.Y, Math.Max(0, frame.Width - 1), Math.Max(0, frame.Height - 1));

            return bitmap;
        }

        private static Rectangle FitAspect(int width, int height, double aspect)
        {
            if (aspect <= 0)
                aspect = 16.0 / 9.0;

            double canvasAspect = (double)width / height;
            if (canvasAspect > aspect)
            {
                int fittedWidth = Math.Max(1, (int)Math.Round(height * aspect));
                return new Rectangle((width - fittedWidth) / 2, 0, fittedWidth, height);
            }

            int fittedHeight = Math.Max(1, (int)Math.Round(width / aspect));
            return new Rectangle(0, (height - fittedHeight) / 2, width, fittedHeight);
        }

        private static void DrawPlacement(Graphics graphics, Image source, Rectangle frame, WallpaperPlacement placement)
        {
            switch (placement)
            {
                case WallpaperPlacement.Stretch:
                    graphics.DrawImage(source, frame);
                    break;

                case WallpaperPlacement.Fit:
                    DrawScaled(graphics, source, frame, useCover: false);
                    break;

                case WallpaperPlacement.Center:
                    {
                        var centered = new Rectangle(
                            frame.X + (frame.Width - source.Width) / 2,
                            frame.Y + (frame.Height - source.Height) / 2,
                            source.Width,
                            source.Height);
                        graphics.DrawImage(source, centered);
                        break;
                    }

                case WallpaperPlacement.Tile:
                    {
                        using var brush = new TextureBrush(source, WrapMode.Tile);
                        brush.TranslateTransform(frame.X, frame.Y);
                        graphics.FillRectangle(brush, frame);
                        break;
                    }

                default:
                    // Fill and Span both crop the image to the screen aspect ratio.
                    DrawScaled(graphics, source, frame, useCover: true);
                    break;
            }
        }

        private static void DrawScaled(Graphics graphics, Image source, Rectangle frame, bool useCover)
        {
            double scaleX = (double)frame.Width / source.Width;
            double scaleY = (double)frame.Height / source.Height;
            double scale = useCover ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);

            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            var destination = new Rectangle(
                frame.X + (frame.Width - width) / 2,
                frame.Y + (frame.Height - height) / 2,
                width,
                height);

            graphics.DrawImage(source, destination);
        }
    }
}
