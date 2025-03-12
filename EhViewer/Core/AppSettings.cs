using Windows.Storage;

namespace EhViewer
{
    public static class AppSettings
    {
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public static bool SNIEnabled
        {
            get => localSettings.Values["SNIEnabled"] as bool? ?? false;
            set => localSettings.Values["SNIEnabled"] = value;
        }
        public static string SNIValue
        {
            get => localSettings.Values["SNIValue"]?.ToString() ?? "";
            set => localSettings.Values["SNIValue"] = value;
        }

        public static bool DOHEnabled
        {
            get => localSettings.Values["DOHEnabled"] as bool? ?? false;
            set => localSettings.Values["DOHEnabled"] = value;
        }
        public static string DOHEndpoint
        {
            get => localSettings.Values["DOHEndpoint"]?.ToString() ?? "";
            set => localSettings.Values["DOHEndpoint"] = value;
        }

        public static string HostsOverride
        {
            get => localSettings.Values["HostsOverride"]?.ToString() ?? "";
            set => localSettings.Values["HostsOverride"] = value;
        }

        public static bool ProxyEnabled
        {
            get => localSettings.Values["ProxyEnabled"] as bool? ?? false;
            set => localSettings.Values["ProxyEnabled"] = value;
        }
        public static string ProxyAddress
        {
            get => localSettings.Values["ProxyAddress"]?.ToString() ?? "";
            set => localSettings.Values["ProxyAddress"] = value;
        }

        public static int MaxImageDownloads
        {
            get
            {
                if (int.TryParse(localSettings.Values["MaxImageDownloads"]?.ToString(), out int result))
                    return result;
                return 1;
            }
            set => localSettings.Values["MaxImageDownloads"] = value.ToString();
        }

        public static string Cookies
        {
            get => localSettings.Values["Cookies"]?.ToString() ?? "";
            set => localSettings.Values["Cookies"] = value;
        }

        public static bool AlternateEndpointEnabled
        {
            get => localSettings.Values["AlternateEndpointEnabled"] as bool? ?? false;
            set => localSettings.Values["AlternateEndpointEnabled"] = value;
        }

        public static bool FilterAds
        {
            get => localSettings.Values["FilterAds"] as bool? ?? false;
            set => localSettings.Values["FilterAds"] = value;
        }
        public static bool HideControversial
        {
            get => localSettings.Values["HideControversial"] as bool? ?? false;
            set => localSettings.Values["HideControversial"] = value;
        }
        public static bool HideComments
        {
            get => localSettings.Values["HideComments"] as bool? ?? false;
            set => localSettings.Values["HideComments"] = value;
        }
    }
}