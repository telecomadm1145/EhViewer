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
using Microsoft.UI.Xaml.Controls;

namespace EhViewer
{
    [AddINotifyPropertyChangedInterface]
    public class MainViewModel
    {
        private EhApi eh = new();
        public bool Loading { get; set; } = false;
        public bool Error { get; set; } = false;
        public EhApi.SearchResult SearchResult { get; set; } = default;
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
        public ICommand Open => new RelayCommand((object? url) =>
        {
            if (url is string s)
            {
                TabView rootFrame = Window.Current.Content as TabView;
                rootFrame.TabItems.Add(new TabViewItem()
                {
                    Header = "漫画",
                    Content = new ComicViewer(new ViewerViewModel(eh, s)),
                });
            }
        });
    }
}
