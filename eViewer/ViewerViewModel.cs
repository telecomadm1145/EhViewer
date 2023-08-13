using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace eViewer
{
    [AddINotifyPropertyChangedInterface]
    internal class ViewerViewModel
    {
        ehApi api;
        [AddINotifyPropertyChangedInterface]
        public class vImage
        {
            public string PageUrl { get; set; }
            public string Title { get; set; }
            public bool Downloaded { get; set; }
            public ImageSource? Source { get; set; }
        }
        public ObservableCollection<vImage> GalleryImages { get; private set; } = new();
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public ViewerViewModel()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        {
            // 留给previewer
        }
        public ViewerViewModel(ehApi api, string url)
        {
            this.api = api;
            _ = Load(url);
        }
        public async Task Load(string url)
        {
            await foreach (var img in api.GetImages(url))
            {
                vImage vi = new();
                vi.PageUrl = img.PageUrl; vi.Title = img.Title; vi.Source = img.Preview;
                GalleryImages.Add(vi);
            }
            Limit limit = new(5);
            foreach (var img in GalleryImages) // this starts the download progress
            {
                _ = Download(limit,img);
            }
        }
        private class Limit
        {
            public Limit(int Max)
            {
                this.Max = Max;
            }
            public int Max = 0;
            public int Value = 0;
            public async Task WaitForAvaliable()
            {
                while (Value >= Max)
                {
                    await Task.Delay(20);
                }
            }
            public async Task Enter()
            {
                await WaitForAvaliable();
                Value++;
            }
            public void Exit()
            {
                Value--;
            }
        }
        private async Task Download(Limit l, vImage img)
        {
            try
            {
                await l.Enter();
                var bigurl = await api.GatherImageLink(img.PageUrl);
            redo:
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(bigurl);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                if (bi.IsDownloading)
                {
                    TaskCompletionSource<bool> tcs = new();
                    bi.DownloadCompleted += (_, _) => { tcs.SetResult(true); };
                    bi.DownloadFailed += (_, _) => { tcs.SetResult(false); };
                    var res = await tcs.Task;
                    if (!res)
                    {
                        await Task.Delay(3000);
                        goto redo;
                    }
                }
                img.Source = bi;
            }
            finally
            {
                l.Exit();
            }
        }
        public vImage? Current { get; set; }
        public int Index { get; set; } = 0;
        public Visibility OverlayOpened { get; set; } = Visibility.Collapsed;
        public ICommand Next => new RelayCommand(async (object? arg) =>
        {
            Index++;
            if (Index < GalleryImages.Count)
            {
                Current = GalleryImages[Index];
            }
            else
            {
                Index = 0;
            }
        });
        public ICommand Open => new RelayCommand(async (object? arg) =>
        {
            if (arg is string s)
            {
                Index = GalleryImages.IndexOf(GalleryImages.First(x => x.PageUrl == s));
                if (Index < GalleryImages.Count && Index >= 0)
                {
                    Current = GalleryImages[Index];
                }
            }
            OverlayOpened = Visibility.Visible;
        });
        public ICommand Close => new RelayCommand(async (object? arg) =>
        {
            OverlayOpened = Visibility.Collapsed;
        });
        public ICommand Prev => new RelayCommand(async (object? arg) =>
        {
            Index--;
            if (Index >= 0)
            {
                Current = GalleryImages[Index];
            }
            else
            {
                Index = 0;
            }
        });
    }
}
