using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace EhViewer
{
    public class InfiniteLoading
    {

        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }

        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(InfiniteLoading), new PropertyMetadata(null, (d, e) =>
            {
                if (d is ScrollViewer a)
                    if (e.NewValue != null)
                    {
                        a.ViewChanged += A_ViewChanged;
                    }
                    else
                    {
                        a.ViewChanged -= A_ViewChanged;
                    }
            }));

        public static bool GetAtBottom(DependencyObject obj)
        {
            return (bool)obj.GetValue(AtBottomProperty);
        }

        public static void SetAtBottom(DependencyObject obj, bool value)
        {
            obj.SetValue(AtBottomProperty, value);
        }

        public static readonly DependencyProperty AtBottomProperty =
    DependencyProperty.RegisterAttached("AtBottom", typeof(bool), typeof(InfiniteLoading), new PropertyMetadata(false));
        private static void A_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var sv = sender as ScrollViewer;
            double verticalOffset = sv.VerticalOffset;
            double extentHeight = sv.ExtentHeight;
            double viewportHeight = sv.ViewportHeight;

            // 判断是否滚动到底部
            bool isAtBottom = verticalOffset >= extentHeight - viewportHeight;
            if (isAtBottom ^ GetAtBottom(sv))
            {
                if (isAtBottom)
                {
                    var cmd = GetCommand(sv);
                    if (cmd != null)
                    {
                        if (cmd.CanExecute(null))
                        {
                            cmd.Execute(null);
                        }
                    }
                }
                SetAtBottom(sv, isAtBottom);
            }
        }
    }
}
