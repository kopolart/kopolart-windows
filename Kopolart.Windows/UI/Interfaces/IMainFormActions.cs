using System;
using System.Windows.Forms;

namespace Kopolart.Windows.UI.Interfaces
{
    public interface IMainFormActions : IWin32Window
    {
        DateTime SessionStartTime { get; }
        Kopolart.Windows.Application.Services.UsageTrackerService UsageTracker { get; }
        Form AsForm { get; }
        void PromptExit();
        void ForceExit();
        void UpdateThemeState(bool isDark);
        void ShowDashboard();
        void ShowWebView(string? targetUrl = null);
        bool IsDashboardVisible { get; }
    }
}
