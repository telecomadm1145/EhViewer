using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EhViewer
{
    internal class SearchHelper
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
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(SearchHelper), new PropertyMetadata(null, (d, e) =>
            {
                if (d is AutoSuggestBox a)
                    if (e.NewValue != null)
                    {
                        a.QuerySubmitted += A_QuerySubmitted;
                    }
                    else
                    {
                        a.QuerySubmitted -= A_QuerySubmitted;
                    }
            }));

        private static void A_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var cmd = GetCommand(sender);
            if (cmd != null)
            {
                if (cmd.CanExecute(args.QueryText))
                {
                    cmd.Execute(args.QueryText);
                }
            }
        }
    }
}
