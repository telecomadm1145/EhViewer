using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace eViewer
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
                ComicViewer cv = new();
                cv.DataContext = new ViewerViewModel(eh, s);
                cv.Show();
            }
        });
    }
}
