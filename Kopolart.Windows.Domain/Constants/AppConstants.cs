namespace Kopolart.Windows.Domain.Constants
{
    public static partial class AppConstants
    {
        public static class Urls
        {
            public const string MainUrl = "https://kopolart.ir/";
            public const string Domain = "kopolart.ir";
            public const string UpdateJsonUrl = "https://kopolart.ir/json/app-updates-windows.json";
            public const string FallbackDownloadUrl = "https://kopolart.ir/download-app/windows";
            public const string DonateUrl = "https://kopolart.ir/donate";
            public const string LicenseUrl = "https://kopolart.ir/license";
            public const string GithubUrl = "https://github.com/kopolart";
            public const string UserLogin = "https://kopolart.ir/login";
            public const string UserRegister = "https://kopolart.ir/register";
            public const string UserProfileBase = "https://kopolart.ir/user/";
            public const string TopBarInfoUrl = "https://kopolart.ir/api/topbar-info";
            public const string RssFeed = "https://kopolart.ir/feed.xml";
            public const string Colors = "https://kopolart.ir/colors";
            public const string ColorPalettes = "https://kopolart.ir/color-palettes";
            public const string Posts = "https://kopolart.ir/posts";
            public const string Wallpapers = "https://kopolart.ir/wallpapers";
            public const string Tools = "https://kopolart.ir/tools";
            public const string PostsSearch = "https://kopolart.ir/posts/search";
            public const string ColorsSearch = "https://kopolart.ir/colors/search";
            public const string ColorPalettesSearch = "https://kopolart.ir/color-palettes/search";
            public const string WallpapersSearch = "https://kopolart.ir/wallpapers/search";
            public const string SearchApi = "https://kopolart.ir/api/search";

            // Virtual host that serves the local offline page inside WebView2.
            // Using a real (trusted) origin lets the offline page post web messages,
            // which about:blank / NavigateToString content cannot do.
            public const string OfflineVirtualHost = "offline.kopolart.local";
            public const string OfflineBaseUrl = "https://" + OfflineVirtualHost + "/";
        }

        public static class Cookies
        {
            public const string ClientUserInfo = "kopol_user";
        }

        public static class Files
        {
            public const string WebViewRuntimeExe = "msedgewebview2.exe";
        }

        public static class Paths
        {
            public const string AppDataFolderName = "Kopolart";
            public const string WebView2UserDataFolder = "WebView2";
        }

        public static class Messages
        {
            public const string RetryConnection = "RETRY_CONNECTION";
        }

        public static class PartialVisitKeys
        {
            public const string WindowsAppBrowse = "app-windows";
            public const string JosephyBrowse = "app-josephy";
        }
    }
}
