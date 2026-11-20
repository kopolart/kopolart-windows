using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Manages loading and applying custom private fonts for the application.
    /// </summary>
    public static class FontManager
    {
        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

        private static readonly PrivateFontCollection PrivateFonts = new();
        private static readonly FontFamily? RegularFontFamily;
        private static readonly FontFamily? BoldFontFamily;
        private static readonly FontFamily? BlackFontFamily;
        private static readonly FontFamily? IconFontFamily;

        // Only one family set is embedded per build: Abar for local builds, Estedad in the
        // public repository (Abar may not be redistributed). Missing entries are skipped.
        private static readonly string[] BundledFontResources =
        {
            "Kopolart.Windows.assets.font.AbarLow-Regular.ttf",
            "Kopolart.Windows.assets.font.AbarMid-Bold.ttf",
            "Kopolart.Windows.assets.font.AbarHigh-Black.ttf",
            "Kopolart.Windows.assets.font.abar-black.ttf",
            "Kopolart.Windows.assets.font.Estedad-Regular.ttf",
            "Kopolart.Windows.assets.font.Estedad-ExtraBold.ttf",
            "Kopolart.Windows.assets.font.Estedad-Black.ttf",
            "Kopolart.Windows.assets.font.kopolart-fa.ttf",
        };

        private static readonly string[] BundledFontFiles =
        {
            "AbarLow-Regular.ttf",
            "AbarMid-Bold.ttf",
            "AbarHigh-Black.ttf",
            "Estedad-Regular.ttf",
            "Estedad-ExtraBold.ttf",
            "Estedad-Black.ttf",
            "kopolart-fa.ttf",
        };

        static FontManager()
        {
            try
            {
                bool loadedAny = false;

                // Load from Embedded Resources (for single-file publish compatibility)
                foreach (var resource in BundledFontResources)
                {
                    loadedAny |= TryLoadFontFromResource(resource);
                }

                // Fallback to disk files if embedded resources aren't found (e.g. debug mode/custom structures)
                if (!loadedAny)
                {
                    string fontDir = Path.Combine(AppContext.BaseDirectory, "assets", "font");
                    foreach (var fileName in BundledFontFiles)
                    {
                        string fontPath = Path.Combine(fontDir, fileName);
                        if (!File.Exists(fontPath))
                        {
                            continue;
                        }

                        PrivateFonts.AddFontFile(fontPath);
                        loadedAny = true;
                    }
                }

                if (loadedAny && PrivateFonts.Families.Length > 0)
                {
                    foreach (var family in PrivateFonts.Families)
                    {
                        if (IsFamily(family, "Abar Low Medium", "Estedad"))
                        {
                            RegularFontFamily ??= family;
                        }
                        else if (IsFamily(family, "Abar Mid ExtraBold", "Estedad ExtraBold"))
                        {
                            BoldFontFamily ??= family;
                        }
                        else if (IsFamily(family, "Abar Mid ExtraBlack", "Estedad Black"))
                        {
                            BlackFontFamily ??= family;
                        }
                        else if (family.Name.Contains("kopolart", StringComparison.OrdinalIgnoreCase))
                        {
                            IconFontFamily ??= family;
                        }
                    }

                    RegularFontFamily ??= PrivateFonts.Families[0];
                    BoldFontFamily ??= (PrivateFonts.Families.Length > 1 ? PrivateFonts.Families[1] : PrivateFonts.Families[0]);
                    BlackFontFamily ??= BoldFontFamily ?? RegularFontFamily;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load custom fonts: {ex}");
            }
        }

        private static bool IsFamily(FontFamily family, params string[] names)
        {
            foreach (var name in names)
            {
                if (family.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryLoadFontFromResource(string resourceName)
        {
            try
            {
                var assembly = typeof(FontManager).Assembly;
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    return false;
                }

                var len = (int)stream.Length;
                var buffer = new byte[len];
                stream.ReadExactly(buffer, 0, len);

                var dataPtr = Marshal.AllocCoTaskMem(len);
                Marshal.Copy(buffer, 0, dataPtr, len);

                // Register with GDI+ (PrivateFontCollection)
                PrivateFonts.AddMemoryFont(dataPtr, len);

                // Register with GDI (required for standard WinForms controls)
                uint pcFonts = 0;
                AddFontMemResourceEx(dataPtr, (uint)len, IntPtr.Zero, ref pcFonts);

                // Note: We don't free dataPtr immediately here because PrivateFontCollection
                // requires the memory block to remain valid while using the font.
                // In a static manager, this memory lives for the lifetime of the application.
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load embedded font resource '{resourceName}': {ex}");
                return false;
            }
        }

        /// <summary>
        /// Gets the loaded custom FontFamily, or falls back to system generic sans-serif.
        /// </summary>
        public static FontFamily FontFamily => RegularFontFamily ?? FontFamily.GenericSansSerif;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string Family, float Size, FontStyle Style), Font> _fontCache = new();

        /// <summary>
        /// Retrieves a cached Font or creates it if not already present.
        /// Reuses GDI handles and prevents handle exhaustion during repaint loops.
        /// </summary>
        private static Font GetOrRecreateFont((string Family, float Size, FontStyle Style) key, Func<Font> factory)
        {
            if (_fontCache.TryGetValue(key, out var cached))
            {
                bool isAlive = false;
                try
                {
                    _ = cached.Height;
                    isAlive = true;
                }
                catch
                {
                    _fontCache.TryRemove(key, out _);
                }

                if (isAlive)
                {
                    return cached;
                }
            }

            var newFont = factory();
            _fontCache[key] = newFont;
            return newFont;
        }

        /// <summary>
        /// Retrieves a cached Font or creates it if not already present.
        /// Reuses GDI handles and prevents handle exhaustion during repaint loops.
        /// </summary>
        public static Font GetFont(float size, FontStyle style = FontStyle.Regular)
        {
            var family = style.HasFlag(FontStyle.Bold)
                ? (BoldFontFamily ?? RegularFontFamily)
                : RegularFontFamily;

            var familyName = family?.Name ?? FontFamily.GenericSansSerif.Name;
            return GetOrRecreateFont((familyName, size, style), () =>
                new Font(family ?? FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point));
        }

        /// <summary>
        /// Retrieves a cached Black Font or creates it if not already present.
        /// </summary>
        public static Font GetBlackFont(float size)
        {
            var family = BlackFontFamily ?? BoldFontFamily ?? RegularFontFamily;
            var familyName = family?.Name ?? FontFamily.GenericSansSerif.Name;
            return GetOrRecreateFont((familyName, size, FontStyle.Regular), () =>
                new Font(family ?? FontFamily.GenericSansSerif, size, FontStyle.Regular, GraphicsUnit.Point));
        }

        /// <summary>
        /// Retrieves a cached Icon Font or creates it if not already present.
        /// </summary>
        public static Font GetIconFont(float size)
        {
            if (IconFontFamily != null)
            {
                var familyName = IconFontFamily.Name;
                return GetOrRecreateFont((familyName, size, FontStyle.Regular), () =>
                    new Font(IconFontFamily, size, FontStyle.Regular, GraphicsUnit.Point));
            }

            return GetOrRecreateFont(("Segoe MDL2 Assets", size, FontStyle.Regular), () =>
                new Font("Segoe MDL2 Assets", size, FontStyle.Regular, GraphicsUnit.Point));
        }

        /// <summary>
        /// Creates a Font using the custom family (bold variant when the style is bold).
        /// Lets callers style freshly created items directly instead of re-walking a
        /// whole control tree with ApplyCustomFont.
        /// </summary>
        public static Font CreateFont(float size, FontStyle style)
        {
            var family = style.HasFlag(FontStyle.Bold)
                ? (BoldFontFamily ?? RegularFontFamily)
                : RegularFontFamily;

            return new Font(family ?? FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
        }

        /// <summary>
        /// Creates a Font using the extra-black display family (Abar or Estedad Black).
        /// Provides bold, clean, and punchy titles without synthetic distortion.
        /// </summary>
        public static Font CreateBlackFont(float size)
        {
            var family = BlackFontFamily ?? BoldFontFamily ?? RegularFontFamily;
            return new Font(family ?? FontFamily.GenericSansSerif, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        /// <summary>
        /// Recursively applies the custom font family to the specified control and all its children, preserving font sizes and styles.
        /// </summary>
        /// <param name="control">The target control to apply the font to.</param>
        public static void ApplyCustomFont(Control? control)
        {
            if (control == null) return;

            // Preserve icon fonts
            if (control.Font.Name.Contains("kopolart", StringComparison.OrdinalIgnoreCase) ||
                control.Font.Name == "Segoe MDL2 Assets")
            {
                return;
            }

            // Preserve black fonts
            if (BlackFontFamily != null && (control.Font.FontFamily.Equals(BlackFontFamily) ||
                string.Equals(control.Font.FontFamily.Name, BlackFontFamily.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            bool isBold = control.Font.Bold;
            var targetFamily = isBold ? BoldFontFamily : RegularFontFamily;

            if (targetFamily != null && !control.Font.FontFamily.Equals(targetFamily))
            {
                try
                {
                    var oldFont = control.Font;
                    bool isInherited = false;
                    if (control.Parent != null && ReferenceEquals(oldFont, control.Parent.Font))
                    {
                        isInherited = true;
                    }

                    control.Font = new Font(targetFamily, control.Font.Size, control.Font.Style, control.Font.Unit);

                    bool isCached = false;
                    foreach (var cached in _fontCache.Values)
                    {
                        if (ReferenceEquals(oldFont, cached))
                        {
                            isCached = true;
                            break;
                        }
                    }

                    if (!isInherited && !isCached && oldFont != null && oldFont != SystemFonts.DefaultFont && oldFont != Control.DefaultFont)
                    {
                        oldFont.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to apply custom font to control {control.Name}: {ex}");
                }
            }

            // Apply to items inside ToolStrips (MenuStrip, StatusStrip, etc.)
            if (control is ToolStrip toolStrip)
            {
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    ApplyCustomFontToItem(item);
                }
            }

            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyCustomFont(child);
            }
        }

        /// <summary>
        /// Applies the custom font family to a ToolStripItem and recursively to its dropdown items.
        /// </summary>
        private static void ApplyCustomFontToItem(ToolStripItem? item)
        {
            if (item == null) return;

            if (item.Font.Name == "Segoe MDL2 Assets" || item.Font.Name == "Segoe UI" || item.Font.Name.Contains("kopolart", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool isBold = item.Font.Bold;
            var targetFamily = isBold ? BoldFontFamily : RegularFontFamily;

            if (targetFamily != null && !item.Font.FontFamily.Equals(targetFamily))
            {
                try
                {
                    var oldFont = item.Font;
                    bool isInherited = false;
                    if (item.Owner != null && ReferenceEquals(oldFont, item.Owner.Font))
                    {
                        isInherited = true;
                    }

                    item.Font = new Font(targetFamily, item.Font.Size, item.Font.Style, item.Font.Unit);

                    bool isCached = false;
                    foreach (var cached in _fontCache.Values)
                    {
                        if (ReferenceEquals(oldFont, cached))
                        {
                            isCached = true;
                            break;
                        }
                    }

                    if (!isInherited && !isCached && oldFont != null && oldFont != SystemFonts.DefaultFont)
                    {
                        oldFont.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to apply custom font to item {item.Name}: {ex}");
                }
            }

            if (item is ToolStripDropDownItem { HasDropDownItems: true } dropDownItem)
            {
                foreach (ToolStripItem subItem in dropDownItem.DropDownItems)
                {
                    ApplyCustomFontToItem(subItem);
                }
            }
        }
    }
}

