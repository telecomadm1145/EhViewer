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


        public static ICommand GetUpdateSuggestionCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(UpdateSuggestionCommandProperty);
        }

        public static void SetUpdateSuggestionCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(UpdateSuggestionCommandProperty, value);
        }

        public static readonly DependencyProperty UpdateSuggestionCommandProperty =
            DependencyProperty.RegisterAttached("UpdateSuggestionCommand", typeof(ICommand), typeof(SearchHelper), new PropertyMetadata(null, (d, e) =>
            {
                if (d is AutoSuggestBox a)
                    if (e.NewValue != null)
                    {
                        a.TextChanged += A_UpdateSuggestionCommand;
                    }
                    else
                    {
                        a.TextChanged -= A_UpdateSuggestionCommand;
                    }
            }));
        private static void A_UpdateSuggestionCommand(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var cmd = GetUpdateSuggestionCommand(sender);
            if (cmd != null)
            {
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    cmd.Execute(sender);
                }
            }
        }
        private static void A_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var cmd = GetCommand(sender);
            if (cmd != null && args.ChosenSuggestion == null)
            {
                if (cmd.CanExecute(args.QueryText))
                {
                    cmd.Execute(args.QueryText);
                }
            }
        }
    }
}
