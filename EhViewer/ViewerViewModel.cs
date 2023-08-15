using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace EhViewer
{
    [AddINotifyPropertyChangedInterface]
    internal class ViewerViewModel
    {
        EhApi api;
        [AddINotifyPropertyChangedInterface]
        public class ViewerImage
        {
            public string PageUrl { get; set; }
            public string Title { get; set; }
            public bool Loading { get; set; } = true;
            public byte[] FullImgData { get; set; }
            public ImageSource? Preview { get; set; }
            public ImageSource? Source { get; set; }
            public double Progress { get; set; }
        }
        public ObservableCollection<ViewerImage> GalleryImages { get; private set; } = new();

        private CancellationTokenSource _cancellationTokenSource = new();
        public bool IsLoading { get; set; } = true;
        public Visibility ShowSaveButton { get; set; } = Visibility.Collapsed;
        public Visibility IsLoadingVisible { get; set; } = Visibility.Visible;
        public ICommand Save => new RelayCommand(async (_) =>
        {
            if (ShowSaveButton == Visibility.Collapsed)
                return;
            FolderPicker picker = new();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null)
                return;
            int i = 0;
            foreach (var item in GalleryImages)
            {
                var file = await folder.CreateFileAsync($"{i}.jpg");
                var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                await stream.WriteAsync(item.FullImgData.AsBuffer());
                stream.Dispose();
                ++i;
            }
            ContentDialog messageDialog = new ContentDialog
            {
                Title = "EhViewer",
                Content = $"保存了{i}图片。",
                CloseButtonText = "关闭"
            };

            await messageDialog.ShowAsync();
        });
        private int RawProgress = 0;
        public double Progress { get; set; } = 0;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public ViewerViewModel()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        {
            // 留给previewer
        }
        public ViewerViewModel(EhApi api, string url)
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
            try
            {
                await foreach (var img in api.GetImages(url))
                {
                    ViewerImage vi = new()
                    {
                        PageUrl = img.PageUrl,
                        Title = img.Title,
                        Preview = img.Preview,
                        Source = img.Preview,
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
                TaskCompletionSource<bool> tcs = new();
                _ = Task.WhenAll(downloadTasks).ContinueWith((t) =>
                {
                    tcs.SetResult(true);
                    Debug.WriteLine("Download finished.");
                });
                await tcs.Task;
                ShowSaveButton = Visibility.Visible;
                IsLoadingVisible = Visibility.Collapsed;
            }
            catch
            {
                Debug.Assert(false);
            }
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
        private async Task Download(Limit l, ViewerImage img, CancellationToken cancel)
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
                try
                {
                    await DownloadImageAsync(bigurl, cancel, img);
                }
                catch
                {
                    cancel.ThrowIfCancellationRequested();
                    Debug.WriteLine($"[{id}]Failed...retrying...");
                    await Task.Delay(9000);
                    goto redo;
                }
                Debug.WriteLine($"[{id}]Downloaded {bigurl}!");
                img.Progress = 100;
                RawProgress++;
                Progress = (double)RawProgress / GalleryImages.Count * 100;
            }
            finally
            {
                Debug.WriteLine($"[{id}]Exiting...");
                l.Exit();
            }
        }

        private async Task DownloadImageAsync(string imageUrl, CancellationToken cancel, ViewerImage img)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);

            var image = new BitmapImage();
            var pv = new BitmapImage();
            pv.DecodePixelWidth = 300;
            var totalBytes = response.Content.Headers.ContentLength ?? 1;
            var bytesRead = 0L;

            using (var stream = new MemoryStream())
            {
                using (var inputStream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[8192];
                    var bytesReadThisTime = 0;

                    while ((bytesReadThisTime = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        bytesRead += bytesReadThisTime;
                        await stream.WriteAsync(buffer, 0, bytesReadThisTime);
                        cancel.ThrowIfCancellationRequested();
                        img.Progress = ((double)bytesRead / totalBytes) * 100;
                    }
                }
                img.FullImgData = stream.ToArray();
                stream.Seek(0, SeekOrigin.Begin);
                await image.SetSourceAsync(stream.AsRandomAccessStream());
                stream.Seek(0, SeekOrigin.Begin);
                await pv.SetSourceAsync(stream.AsRandomAccessStream());
            }
            img.Source = image;
            img.Preview = pv;
        }
        public ViewerImage? Current { get; set; }
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
