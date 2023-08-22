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
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Imaging;
using System.Net.Http;
using System.IO;

namespace EhViewer
{
    [AddINotifyPropertyChangedInterface]
    public class MainViewModel
    {
        private EhApi eh = new();
        public bool Loading { get; set; } = false;
        public bool Error { get; set; } = false;
        [AddINotifyPropertyChangedInterface]
        public class Entry
        {
            public string Type { get; set; }
            public DateTime PublishTime { get; set; }
            public string Name { get; set; }
            public string[] Tags { get; set; }
            public double Rating { get; set; }
            public string Uploader { get; set; }
            public int Pages { get; set; }
            public string Url { get; set; }
            public BitmapImage Preview { get; set; }
            public double Height { get; set; }
            public double Width => 300;
        }
        public ObservableCollection<Entry> Entries { get; set; } = new();
        public string NextUrl { get; set; }
        public EhApi.SearchResult SearchResult { get; set; } = default;
        public List<string> Suggestion { get; set; } = new() { "114514", "1919810" };
        public ICommand Search => new RelayCommand(async (object? url) =>
        {
            if (url is string s && !Loading)
            {
                try
                {
                    Loading = true;
                    Entries.Clear();
                    var u = "https://e-hentai.org/?f_search=" + Uri.EscapeDataString(s);
                    await Load(u);
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

        private async Task Load(string u)
        {
            var hc = new HttpClient();
            var sr = await eh.Search(u);
            NextUrl = sr.NextUrl;
            foreach (var entry in sr.Entries)
            {
                Entry e = new();
                e.Type = entry.Type;
                e.PublishTime = entry.PublishTime;
                e.Name = entry.Name;
                e.Tags = entry.Tags;
                e.Rating = entry.Rating;
                e.Uploader = entry.Uploader;
                e.Pages = entry.Pages;
                e.Url = entry.Url;
                e.Height = entry.PreviewHeight / entry.PreviewWidth * e.Width;
                Entries.Add(e);
                _ = LoadPreview(hc, entry, e);
            }
        }

        private static async Task LoadPreview(HttpClient hc, EhApi.SearchEntry entry, Entry e)
        {
            BitmapImage bi = new();
            var stm = await hc.GetStreamAsync(entry.PreviewUrl);
            MemoryStream ms = new();
            await stm.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            await bi.SetSourceAsync(ms.AsRandomAccessStream());
            e.Preview = bi;
        }

        public ICommand InfiniteLoad => new RelayCommand(async (object? url) =>
        {
            if (string.IsNullOrEmpty(NextUrl))
                return;
            if (Loading)
                return;
            try
            {
                Loading = true;
                await Load(NextUrl);
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
