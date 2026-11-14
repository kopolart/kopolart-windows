using Microsoft.Web.WebView2.WinForms;

namespace Kopolart.Windows.UI.Interfaces
{
    /// <summary>
    /// Abstraction for navigation-related operations used by ConnectionRetryHandler.
    /// </summary>
    public interface INavigationController
    {
        bool EnableOfflinePage { get; set; }
        void NavigateToOrigin();
        WebView2? WebView { get; }
    }
}

