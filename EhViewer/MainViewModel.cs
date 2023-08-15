using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace EhViewer
{
    [AddINotifyPropertyChangedInterface]
    public class MainViewModel
    {
        private ehApi eh = new();
        public bool Loading { get; set; } = false;
        public bool Error { get; set; } = false;
        public ehApi.SearchResult SearchResult { get; set; } = default;
        public ICommand Search => new RelayCommand(async (object? url) =>
        {
            if (url is string s && !Loading)
            {
                try
                {
                    Loading = true;
                    SearchResult = await eh.Search("https://e-hentai.org/?f_search=" + Uri.EscapeDataString(s));
                    Error = false;
                }
                catch
                {
                    Error = true;
                }
                finally
                {
                    Loading = false;
                }
            }
        });
        public ICommand To => new RelayCommand(async (object? url) =>
        {
            if (url is string s && !Loading)
            {
                try
                {
                    Loading = true;
                    SearchResult = await eh.Search(s);
                    Error = false;
                }
                catch
                {
                    Error = true;
                }
                finally
                {
                    Loading = false;
                }
            }
        });
        public ICommand Open => new RelayCommand(async (object? url) =>
        {
            if (url is string s)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(ComicViewer), new ViewerViewModel(eh, s), 
                    new SlideNavigationTransitionInfo() { 
                    Effect = SlideNavigationTransitionEffect.FromRight 
                });
            }
        });
    }
}
