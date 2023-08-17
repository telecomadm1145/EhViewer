using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
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
    public sealed partial class ComicViewer : Page
    {
        private ViewerViewModel vvm;
        internal ComicViewer(ViewerViewModel vvm)
        {
            this.vvm = vvm;
            Unloaded += ComicViewer_Unloaded;
            DataContext = vvm;
            this.InitializeComponent();
        }

        private void ComicViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            vvm?.Cancel();
        }
        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.F11)
            {
                e.Handled = true;
                ApplicationView view = ApplicationView.GetForCurrentView();
                if (view.IsFullScreenMode)
                {
                    view.ExitFullScreenMode();
                }
                else
                {
                    view.TryEnterFullScreenMode();
                }
            }
        }
    }
}
