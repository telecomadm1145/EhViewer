using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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
            public bool Loading { get; set; } = true;
            public ImageSource? Source { get; set; }
            public double Progress { get; set; }
        }
        public ObservableCollection<vImage> GalleryImages { get; private set; } = new();

        private CancellationTokenSource _cancellationTokenSource = new();
        public bool IsLoading { get; set; } = true;
        private int RawProgress = 0;
        public double Progress { get; set; } = 0;

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
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }
        public async Task Load(string url)
        {
            await foreach (var img in api.GetImages(url))
            {
                vImage vi = new()
                {
                    PageUrl = img.PageUrl,
                    Title = img.Title,
                    Source = img.Preview
                };
                GalleryImages.Add(vi);
            }
            IsLoading = false;
            List<Task> downloadTasks = new();
            Limit limit = new(5);
            foreach (var img in GalleryImages) // this starts the download progress
            {
                downloadTasks.Add(Download(limit, img, _cancellationTokenSource.Token));
                await Task.Delay(1000);
            }
            _ = Task.WhenAll(downloadTasks).ContinueWith((t) => {
                Debug.WriteLine("Download finished.");
            });
        }
        private class Limit
        {
            public Limit(int Max)
            {
                this.Max = Max;
            }
            public int Max = 0;
            public volatile int Value = 0;
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
        private async Task Download(Limit l, vImage img, CancellationToken cancel)
        {
            int id = DateTime.Now.GetHashCode() & 0xFFF;
            Debug.WriteLine($"[{id}]Trying to download {img.PageUrl}...(In Queue)");
            try
            {
                await l.Enter();
                img.Loading = false;
                Debug.WriteLine($"[{id}]Gathering {img.PageUrl}...");
                var bigurl = await api.GatherImageLink(img.PageUrl);
                Debug.WriteLine($"[{id}]Downloading {bigurl}...");
            redo:
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(bigurl);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                if (bi.IsDownloading)
                {
                    TaskCompletionSource<bool>? tcs = new();
                    bi.DownloadProgress += (_, e) => { img.Progress = e.Progress; };
                    bi.DownloadCompleted += (_, _) => { tcs?.SetResult(true); };
                    bi.DownloadFailed += (_, _) => { tcs?.SetResult(false); };
                    cancel.Register(() => { tcs?.SetResult(true); });
                    var res = await tcs.Task;
                    tcs = null;
                    cancel.ThrowIfCancellationRequested();
                    if (!res)
                    {
                        Debug.WriteLine($"[{id}]Download {bigurl} failed,retrying...");
                        await Task.Delay(3000, cancel);
                        goto redo;
                    }
                }
                Debug.WriteLine($"[{id}]Downloaded {bigurl}!");
                //bi.Freeze();
                img.Progress = 100;
                img.Source = bi;
                RawProgress++;
                Progress = (double)RawProgress / GalleryImages.Count * 100;
            }
            finally
            {
                Debug.WriteLine($"[{id}]Exiting...");
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
