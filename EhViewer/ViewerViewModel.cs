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
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static EhViewer.MainViewModel;

namespace EhViewer
{
    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
    [AddINotifyPropertyChangedInterface]
    internal class ViewerViewModel
    {
        EhApi api;

        public Dictionary<string, string> Details { get; set; }
        public string Title { get; set; }
        public string RawTitle { get; set; }
        public double Rating { get; set; } = 5;
        public ImageSource? Cover { get; set; }
        public int RatingCount { get; set; }
        public string Publisher { get; set; }
        public ObservableCollection<EhApi.GalleryInfo.Comment> Comments { get; set; } = new();
        public Dictionary<string, List<string>> Tags { get; set; }

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
        public Visibility GalleryViewVisible { get; set; } = Visibility.Collapsed;
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

        public ICommand OpenGalleryView => new RelayCommand((_) =>
        {
            GalleryViewVisible = Visibility.Visible;
        });
        public ICommand CloseGalleryView => new RelayCommand((_) =>
        {
            GalleryViewVisible = Visibility.Collapsed;
        });
        private int RawProgress = 0;
        public double Progress { get; set; } = 0;

        public string org_url { get; set; }

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        public ViewerViewModel()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        {
            // 留给previewer
        }
        public ViewerViewModel(EhApi api, ImageSource initalPreview, string url)
        {
            this.api = api;
            _ = Load(url);
            org_url = url;
            Cover = initalPreview;
        }
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }
        public async Task Load(string url)
        {
            bool offline_mode = false;
            try
            {
                await OfflineDb.Instance.Load();
                var gi = OfflineDb.Instance.Infos.FirstOrDefault(x => new Uri(x.Url).AbsolutePath == new Uri(url).AbsolutePath);

                EhApi.GalleryInfo info = default;
                if (OfflineDb.Instance.OfflineMode)
                {
                    offline_mode = true;
                }
                if (!offline_mode)
                {
                    try
                    {
                        info = await api.GetGalleryInfo(url);
                    }
                    catch
                    {
                        offline_mode = true;
                        if (gi == null)
                            return;
                        info = gi.GalleryInfo;
                    }
                }
                else
                {
                    if (gi == null)
                        return;
                    info = gi.GalleryInfo;
                }

                var ogi = new OfflineGalleryInfo();
                ogi.Url = url;
                ogi.GalleryInfo = info;

                Details = info.Details;
                Title = info.Title;
                RawTitle = string.IsNullOrWhiteSpace(info.RawTitle) ? info.Title : info.RawTitle;
                Rating = info.Rating;
                RatingCount = info.RateCount;
                Tags = info.Tags;
                Publisher = info.Publisher;
                Comments.AddRange(info.comments ?? new());
                if (!offline_mode)
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
                            ogi.Images.Add(img.PageUrl);
                        }
                    }
                    catch
                    {
                        offline_mode = true;
                        if (gi == null)
                            return;
                        foreach (var img in gi.Images)
                        {
                            ViewerImage vi = new()
                            {
                                PageUrl = img,
                            };
                            GalleryImages.Add(vi);
                        }
                    }
                }
                else
                {
                    if (gi == null)
                        return;
                    foreach (var img in gi.Images)
                    {
                        ViewerImage vi = new()
                        {
                            PageUrl = img,
                        };
                        GalleryImages.Add(vi);
                    }
                }
                if (!offline_mode)
                {
                    Debug.WriteLine("Updated OfflineDb.");
                    await OfflineDb.Instance.UpdateGalleryInfo(ogi);
                }
                IsLoading = false;
                bool is_cover_pushed = false;
                List<Task> downloadTasks = new();
                Limit limit = Limit.g_limit;
                foreach (var img in GalleryImages) // this starts the download progress
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var tsk = Download(limit, img, _cancellationTokenSource.Token);
                    if (!is_cover_pushed)
                    {
                        _ = tsk.ContinueWith((t) =>
                        {
                            Cover = img.Source;
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        is_cover_pushed = true;
                    }
                    downloadTasks.Add(tsk);
                    // await Task.Delay(1000, _cancellationTokenSource.Token);
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
                byte[] cache = null;
                cache = await CachingService.Instance.GetFromCache("Image!!" + new Uri(img.PageUrl).AbsolutePath);
                if (cache != null)
                {
                    img.Progress = 100;
                    img.FullImgData = cache;
                    var image = new BitmapImage();
                    var pv = new BitmapImage();
                    pv.DecodePixelWidth = 200;
                    using (var stream = new MemoryStream(cache))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        await image.SetSourceAsync(stream.AsRandomAccessStream());
                        stream.Seek(0, SeekOrigin.Begin);
                        await pv.SetSourceAsync(stream.AsRandomAccessStream());
                    }
                    img.Preview = pv;
                    img.Source = image;
                }
                else
                {
                    Debug.WriteLine($"[{id}]Gathering {img.PageUrl}...");
                    var imageUrl = await api.GatherImageLink(img.PageUrl);
                    Debug.WriteLine($"[{id}]Downloading {imageUrl}...");
                redo:
                    try
                    {
                        await DownloadImageAsync(imageUrl, cancel, img);
                    }
                    catch
                    {
                        cancel.ThrowIfCancellationRequested();
                        Debug.WriteLine($"[{id}]Failed...retrying...");
                        await Task.Delay(4000);
                        goto redo;
                    }
                    _ = CachingService.Instance.SaveToCache("Image!!" + new Uri(img.PageUrl).AbsolutePath, img.FullImgData);
                    Debug.WriteLine($"[{id}]Downloaded {imageUrl}!");
                    img.Progress = 100;
                }
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
            pv.DecodePixelWidth = 200;
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
        public ICommand Next => new RelayCommand((object? arg) =>
        {
            if (Index + 1 < GalleryImages.Count)
            {
                Index++;
                Current = GalleryImages[Index];
            }
        });
        public ICommand Open => new RelayCommand((object? arg) =>
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
        public ICommand Close => new RelayCommand((object? arg) =>
        {
            OverlayOpened = Visibility.Collapsed;
        });
        public ICommand Prev => new RelayCommand((object? arg) =>
        {
            if (Index > 0)
            {
                Index--;
                Current = GalleryImages[Index];
            }
        });

        public ICommand CopyCurrent => new RelayCommand(async (object? arg) =>
        {
            try
            {
                var tempFile = await ApplicationData.Current.TemporaryFolder
.CreateFileAsync("CbExch", CreationCollisionOption.ReplaceExisting);
                var stream = await tempFile.OpenAsync(FileAccessMode.ReadWrite);
                await stream.AsStream().WriteAsync(GalleryImages[Index].FullImgData, 0, GalleryImages[Index].FullImgData.Length);
                var dataPackage = new DataPackage();
                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(tempFile));
                Clipboard.SetContent(dataPackage);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                // 处理异常，可以根据需要显示错误消息
                Debug.WriteLine($"Copy image failed: {ex.Message}");
            }
        });


        public ICommand CopyLink => new RelayCommand((object? _) =>
        {
            var dp = new DataPackage();
            dp.SetText(org_url);
            Clipboard.SetContent(dp);
        });
        public ICommand ShareLink => new RelayCommand((object? _) =>
        {

        });
        public ICommand OpenInBrowser => new RelayCommand(async (object? arg) =>
        {
            await Launcher.LaunchUriAsync(new Uri(GalleryImages[Index].PageUrl));
        });

    }
}
