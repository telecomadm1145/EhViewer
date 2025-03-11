using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace EhViewer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Settings : Page,ICloseable
    {
        public Settings()
        {
            this.InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 读取存储的设置，并显示到控件中
            CheckboxSNI.IsChecked = AppSettings.SNIEnabled;
            TextBoxSNI.Text = AppSettings.SNIValue;

            CheckboxDOH.IsChecked = AppSettings.DOHEnabled;
            TextBoxDOHEndpoint.Text = AppSettings.DOHEndpoint;

            TextBoxHosts.Text = AppSettings.HostsOverride;

            CheckboxProxy.IsChecked = AppSettings.ProxyEnabled;
            TextBoxProxy.Text = AppSettings.ProxyAddress;

            TextBoxMaxDownloads.Text = AppSettings.MaxImageDownloads.ToString();

            TextBoxCookies.Text = AppSettings.Cookies;

            CheckboxAlternateEndpoint.IsChecked = AppSettings.AlternateEndpointEnabled;

            CheckboxFilterAds.IsChecked = AppSettings.FilterAds;
            CheckboxHideControversial.IsChecked = AppSettings.HideControversial;
            CheckboxHideComments.IsChecked = AppSettings.HideComments;
        }

        private void SaveSettings()
        {
            // 保存控件内的设置至 LocalSettings
            AppSettings.SNIEnabled = CheckboxSNI.IsChecked ?? false;
            AppSettings.SNIValue = TextBoxSNI.Text;

            AppSettings.DOHEnabled = CheckboxDOH.IsChecked ?? false;
            AppSettings.DOHEndpoint = TextBoxDOHEndpoint.Text;

            AppSettings.HostsOverride = TextBoxHosts.Text;

            AppSettings.ProxyEnabled = CheckboxProxy.IsChecked ?? false;
            AppSettings.ProxyAddress = TextBoxProxy.Text;

            if (int.TryParse(TextBoxMaxDownloads.Text, out int maxDownloads))
            {
                AppSettings.MaxImageDownloads = maxDownloads;
            }

            AppSettings.Cookies = TextBoxCookies.Text;

            AppSettings.AlternateEndpointEnabled = CheckboxAlternateEndpoint.IsChecked ?? false;

            AppSettings.FilterAds = CheckboxFilterAds.IsChecked ?? false;
            AppSettings.HideControversial = CheckboxHideControversial.IsChecked ?? false;
            AppSettings.HideComments = CheckboxHideComments.IsChecked ?? false;
        }
        public void Close()
        {
            SaveSettings();
        }
    }
}
