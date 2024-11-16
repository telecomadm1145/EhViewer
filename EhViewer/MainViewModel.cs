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
using Windows.UI.Xaml.Data;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Input;
using static EhViewer.MainViewModel;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using Windows.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;

namespace EhViewer
{
    public class SuggestedTag
    {
        public string Namespace { get; set; }
        public string Tag { get; set; }
        public string DisplayText { get; set; }
        public string Raw { get; set; }
        public string Final { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Final;
        }
    }
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
                    var u = $"https://e-hentai.org/?f_cats={Categories}&f_search={Uri.EscapeDataString(s)}";
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
        public List<SuggestedTag> Suggestions { get; set; } = new();

        public ICommand SearchSuggest => new RelayCommand((object? a) =>
        {
            var box = (AutoSuggestBox)a;
            var originalText = box.Text;
            var currentQuery = GetCurrentQuery(originalText);

            if (string.IsNullOrWhiteSpace(currentQuery))
            {
                box.ItemsSource = null;
                return;
            }

            var suggestions = new List<SuggestedTag>();
            var (searchText, prefix, suffix, isExact, isExclude, isOr, hasWildcard) = ParseQuery(currentQuery);
            searchText = searchText.ToLowerInvariant();

            foreach (var @namespace in EhTagTranslation.Instance.Data)
            {
                if (@namespace.Name == "rows")
                    continue;

                foreach (var tagPair in @namespace.Data)
                {
                    if (IsMatchingTag(tagPair, @namespace, searchText, isExact, hasWildcard))
                    {
                        var semanticSuffix = BuildSemanticSuffix(isExact, isExclude, isOr, hasWildcard);
                        var displayText = $"{@namespace.Info.Abbr}:{tagPair.Value.Name.Text}{semanticSuffix}";

                        var rawTag = BuildRawTag(@namespace.Info.Abbr, tagPair.Key, prefix, suffix);
                        var finalTag = BuildFinalTag(originalText, rawTag);

                        suggestions.Add(new SuggestedTag
                        {
                            Namespace = @namespace.Name,
                            Tag = tagPair.Key,
                            DisplayText = displayText,
                            Description = tagPair.Value.Description.Text,
                            Raw = rawTag,
                            Final = finalTag,
                        });
                    }
                }
            }

            var limitedSuggestions = suggestions.Take(10).ToList();
            box.ItemsSource = limitedSuggestions;
        });

        private string GetCurrentQuery(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            // 处理不完整的引号
            var quoteCount = text.Count(c => c == '"');
            if (quoteCount % 2 != 0)
            {
                // 如果有未闭合的引号，添加一个闭合引号
                text += "\"";
            }

            var queries = SplitIntoQueries(text);
            return queries.LastOrDefault() ?? "";
        }

        private List<string> SplitIntoQueries(string text)
        {
            var queries = new List<string>();
            var currentQuery = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    currentQuery.Append(c);
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (currentQuery.Length > 0)
                    {
                        queries.Add(currentQuery.ToString().Trim());
                        currentQuery.Clear();
                    }
                }
                else
                {
                    currentQuery.Append(c);
                }
            }

            if (currentQuery.Length > 0)
                queries.Add(currentQuery.ToString().Trim());

            return queries;
        }

        private (string searchText, string prefix, string suffix, bool isExact, bool isExclude, bool isOr, bool hasWildcard) ParseQuery(string query)
        {
            var searchText = query;
            var prefix = "";
            var suffix = "";
            var isExact = false;
            var isExclude = false;
            var isOr = false;
            var hasWildcard = false;

            // 处理引号
            if (searchText.StartsWith("\"") && searchText.EndsWith("\""))
            {
                searchText = searchText.Substring(1, searchText.Length - 2);
                isExact = true;
            }

            // 处理排除
            if (searchText.StartsWith("-"))
            {
                isExclude = true;
                prefix = "-";
                searchText = searchText.Substring(1);
            }

            // 处理OR
            if (searchText.StartsWith("~"))
            {
                isOr = true;
                prefix = "~";
                searchText = searchText.Substring(1);
            }

            // 处理通配符
            if (searchText.EndsWith("*") || searchText.EndsWith("%"))
            {
                hasWildcard = true;
                suffix = searchText[searchText.Length - 1].ToString();
                searchText = searchText.Substring(0, searchText.Length - 1);
            }

            // 处理精确匹配
            if (searchText.EndsWith("$"))
            {
                isExact = true;
                suffix = "$";
                searchText = searchText.Substring(0, searchText.Length - 1);
            }

            return (searchText, prefix, suffix, isExact, isExclude, isOr, hasWildcard);
        }

        private bool IsMatchingTag(KeyValuePair<string, EhTag> tagPair, EhMainTag @namespace, string searchText, bool isExact, bool hasWildcard)
        {
            var tagKey = tagPair.Key.ToLowerInvariant();
            var tagName = tagPair.Value.Name.Text.ToLowerInvariant();
            var ind = searchText.IndexOf(":");
            var queryNamesp = "";
            var queryText = "";
            if (ind != -1)
            {
                queryNamesp = searchText.Substring(0, ind);
                queryText = searchText.Substring(ind+1);
            }
            else
            {
                queryText = searchText;
            }
            if (!string.IsNullOrWhiteSpace(queryNamesp))
            {
                if (queryNamesp.ToLower() != @namespace.Info.Abbr &&
                    queryNamesp.ToLower() != @namespace.Info.Name) {
                    return false;
                }
            }
            return tagKey.Contains(queryText) || tagName.Contains(queryText);
        }

        private string BuildSemanticSuffix(bool isExact, bool isExclude, bool isOr, bool hasWildcard)
        {
            var semantics = new List<string>();

            if (isExact)
                semantics.Add("(精确匹配)");
            if (isExclude)
                semantics.Add("(排除)");
            if (isOr)
                semantics.Add("(或)");
            if (hasWildcard)
                semantics.Add("(通配符)");

            return semantics.Count > 0 ? " " + string.Join(" ", semantics) : "";
        }

        private string BuildRawTag(string abbr, string tag, string prefix, string suffix)
        {
            return $"{prefix}\"{abbr}:{tag}\"{suffix}";
        }

        private string BuildFinalTag(string originalText, string rawTag)
        {
            var queries = SplitIntoQueries(originalText)
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .ToList();

            // 如果queries为空，直接返回rawTag
            if (!queries.Any())
                return rawTag;

            // 替换最后一个query为rawTag
            if (queries.Count > 0)
                queries[queries.Count - 1] = rawTag;

            // 使用单个空格连接所有query
            return string.Join(" ", queries);
        }

        private string NormalizeSpaces(string text)
        {
            // 移除开头和结尾的空格
            text = text.Trim();

            // 将多个连续空格替换为单个空格
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            return text;
        }

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
            var dat = await CachingService.Instance.GetFromCache("PreviewImg!!" + entry.PreviewUrl);
            MemoryStream ms;
            if (dat != null)
            {
                ms = new MemoryStream(dat);
                ms.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                ms = new();
                var stm = await hc.GetStreamAsync(entry.PreviewUrl);
                await stm.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                await CachingService.Instance.SaveToCache("PreviewImg!!" + entry.PreviewUrl, ms.GetBuffer());
            }
            await bi.SetSourceAsync(ms.AsRandomAccessStream());
            e.Preview = bi;
        }
        public int Categories { get; set; }
        public ICommand CheckCategory => new RelayCommand((object? i) =>
        {
            Categories ^= int.Parse((string)i);
        });
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
        public ICommand CopyImage => new RelayCommand(async (object? img) =>
        {
            if (img is ImageSource imageSource)
            {
                var storageFile = await ConvertImageSourceToStorageFile(imageSource);
                if (storageFile != null)
                {
                    var dataPackage = new DataPackage();
                    dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(storageFile));
                    Clipboard.SetContent(dataPackage);
                }
            }
        });

        private async Task<StorageFile?> ConvertImageSourceToStorageFile(ImageSource imageSource)
        {
            try
            {
                if (imageSource is BitmapImage bitmapImage)
                {
                    // 创建临时文件
                    var tempFile = await ApplicationData.Current.TemporaryFolder
                        .CreateFileAsync(imageSource.GetHashCode() + DateTime.Now.Ticks + ".png", CreationCollisionOption.ReplaceExisting);

                    using (var stream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        // 创建编码器
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

                        // 如果 BitmapImage 来自 URI
                        if (bitmapImage.UriSource != null)
                        {
                            var file = await StorageFile.GetFileFromApplicationUriAsync(bitmapImage.UriSource);
                            using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                            {
                                var decoder = await BitmapDecoder.CreateAsync(fileStream);
                                var pixelData = await decoder.GetPixelDataAsync();

                                encoder.SetPixelData(
                                    decoder.BitmapPixelFormat,
                                    decoder.BitmapAlphaMode,
                                    decoder.PixelWidth,
                                    decoder.PixelHeight,
                                    decoder.DpiX,
                                    decoder.DpiY,
                                    pixelData.DetachPixelData()
                                );
                            }
                        }
                        await encoder.FlushAsync();
                    }

                    return tempFile;
                }
            }
            catch (Exception)
            {
                // 处理异常
            }

            return null;
        }
        public ICommand Open => new RelayCommand((object? url) =>
        {
            if (url is Entry s)
            {
                MainWindow rootFrame = Window.Current.Content as MainWindow;
                ViewerViewModel vvm = new ViewerViewModel(eh, s.Preview, s.Url);
                var item = new TabViewItem()
                {
                    Content = new ComicViewer(vvm),
                };
                Binding bd = new();
                bd.Source = vvm;
                bd.Path = new("RawTitle");
                item.SetBinding(TabViewItem.HeaderProperty, bd);
                rootFrame.NewTab(item);
            }
        });
    }
}
