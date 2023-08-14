using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace eViewer
{
    /// <summary>
    /// ComicViewer.xaml 的交互逻辑
    /// </summary>
    public partial class ComicViewer : Window
    {
        private ViewerViewModel vvm;
        internal ComicViewer(ViewerViewModel vvm)
        {
            InitializeComponent();
            this.vvm = vvm;
            DataContext = vvm;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) {
                if (Topmost)
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Normal;
                    Topmost = false;
                }
                else
                {
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                    Topmost = true;
                    Activate();
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            vvm.Cancel();
        }
    }
}
