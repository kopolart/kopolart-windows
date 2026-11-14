using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Helpers
{
    public static class WebViewDeviceEmulator
    {
        public static async Task UpdateDeviceEmulationAsync(CoreWebView2? coreWebView2, string mode)
        {
            if (coreWebView2 == null) return;

            try
            {
                coreWebView2.Settings.UserAgent = mode switch
                {
                    "tablet" => "Mozilla/5.0 (iPad; CPU OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1",
                    "mobile" => "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
                    _ => null
                };

                if (mode == "mobile")
                {
                    string metricsArgs = "{\"width\":375,\"height\":0,\"deviceScaleFactor\":2,\"mobile\":true}";
                    await coreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride", metricsArgs);

                    string touchArgs = "{\"enabled\":true,\"configuration\":\"mobile\"}";
                    await coreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setTouchEmulationEnabled", touchArgs);
                }
                else if (mode == "tablet")
                {
                    string metricsArgs = "{\"width\":768,\"height\":0,\"deviceScaleFactor\":2,\"mobile\":true}";
                    await coreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride", metricsArgs);

                    string touchArgs = "{\"enabled\":true,\"configuration\":\"mobile\"}";
                    await coreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setTouchEmulationEnabled", touchArgs);
                }
                else
                {
                    await coreWebView2.CallDevToolsProtocolMethodAsync("Emulation.clearDeviceMetricsOverride", "{}");
                    await coreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setTouchEmulationEnabled", "{\"enabled\":false}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set Device Metrics Emulation: {ex.Message}");
            }
        }

        public static void UpdateLayoutBounds(WebView2? webView, string mode)
        {
            if (webView == null || webView.IsDisposed || webView.Parent == null) return;

            if (webView.InvokeRequired)
            {
                try
                {
                    webView.BeginInvoke(new Action(() => UpdateLayoutBounds(webView, mode)));
                }
                catch
                {
                    // Ignore if thread/dispose race
                }
                return;
            }

            if (mode == "mobile" || mode == "tablet")
            {
                webView.Dock = DockStyle.None;
                int targetWidth = (mode == "mobile") ? 375 : 768;
                int parentWidth = webView.Parent.ClientRectangle.Width;
                int parentHeight = webView.Parent.ClientRectangle.Height;

                int width = Math.Min(targetWidth, parentWidth);
                int height = parentHeight;
                int x = (parentWidth - width) / 2;
                int y = 0;

                webView.Bounds = new Rectangle(x, y, width, height);
            }
            else
            {
                webView.Dock = DockStyle.Fill;
            }
        }
    }
}
