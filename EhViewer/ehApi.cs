using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace EhViewer
{
    public class EhApi
    {
        public Uri Endpoint { get; set; } = new Uri("https://e-hentai.org");
        private HttpClient client = new();
        public struct SearchEntry
        {
            public string Type { get; set; }
            public DateTime PublishTime { get; set; }
            public string Name { get; set; }
            public string[] Tags { get; set; }
            public double Rating { get; set; }
            public string Uploader { get; set; }
            public int Pages { get; set; }
            public string Url { get; set; }
            public string PreviewUrl { get; set; }
            public double PreviewHeight { get; set; }
            public double PreviewWidth { get; set; }
        }
        public struct SearchResult
        {
            public List<SearchEntry> Entries { get; set; }
            public string StartUrl { get; set; }
            public string PrevUrl { get; set; }
            public string NextUrl { get; set; }
            public string LastUrl { get; set; }
        }
        public struct SearchOptions
        {
            public string Query { get; set; }

        }
        public async Task<SearchResult> Search(string url)
        {
            SearchResult res = default;
            res.Entries = new();
            var content = await client.GetAsync(url);
            content.EnsureSuccessStatusCode();
            HtmlDocument doc = new();
            doc.Load(await content.Content.ReadAsStreamAsync());
            var nav = doc.DocumentNode.SelectSingleNode("//body//div[2]//div[2]//div[3]");
            res.StartUrl = WebUtility.HtmlDecode(nav?.SelectSingleNode(".//div[2]//a")?.GetAttributeValue("href", null));
            res.PrevUrl = WebUtility.HtmlDecode(nav?.SelectSingleNode(".//div[3]//a")?.GetAttributeValue("href", null));
            res.NextUrl = WebUtility.HtmlDecode(nav?.SelectSingleNode(".//div[5]//a")?.GetAttributeValue("href", null));
            res.LastUrl = WebUtility.HtmlDecode(nav?.SelectSingleNode(".//div[6]//a")?.GetAttributeValue("href", null));
            if (nav == null)
                return res;
            var table = doc.DocumentNode.SelectNodes("//table[1]")[1];
            foreach (var item in table.ChildNodes.Skip(1))
            {
                SearchEntry se = default;
                var query = item.SelectSingleNode(".//td[3]//a//div[1]");
                if (query == null)
                    continue;
                se.Name = query.InnerText;
                se.Url = query.ParentNode.GetAttributes().First(x => x.Name == "href").Value;
                query = item.SelectSingleNode(".//td[3]//a//div[2]");
                if (query == null)
                    continue;
                se.Tags = query.ChildNodes.Select(x => x.InnerText).ToArray();
                query = item.SelectSingleNode(".//td[1]//div");
                if (query == null)
                    continue;
                se.Type = query.InnerText;
                query = item.SelectSingleNode(".//td[2]//div[3]//div[1]");
                if (query == null)
                    continue;
                DateTime time;
                DateTime.TryParse(query.InnerText, out time);
                se.PublishTime = time;
                query = item.SelectSingleNode(".//td[2]//div[3]//div[2]"); // Star
                if (query == null)
                    continue;
                var style = query.GetAttributeValue("style", "");
                var match = Regex.Match(style, "background-position:(-?\\d+)px (-?\\d+)px;");
                if (match.Success)
                {
                    int val, val2 = 0;
                    int.TryParse(match.Groups[1].Value, out val);
                    int.TryParse(match.Groups[2].Value, out val2);
                    if (val2 != -1)
                    {
                        val -= 8;
                    }
                    se.Rating = (80 + val) / 16.0;
                }
                query = item.SelectSingleNode(".//td[4]//div[1]//a[1]");
                if (query == null)
                    continue;
                se.Uploader = query.InnerText;
                query = item.SelectSingleNode(".//td[4]//div[2]");
                int pages = 0;
                int.TryParse(query.InnerText.Split(' ')[0], out pages);
                se.Pages = pages;
                query = item.SelectSingleNode(".//td[2]//div[2]//div[1]//img");
                if (query == null)
                    continue;
                style = query.GetAttributeValue("style", "");
                match = Regex.Match(style, "height:(-?\\d+)px");
                if (match.Success)
                {
                    double a = 0;
                    double.TryParse(match.Groups[1].Value, out a);
                    se.PreviewHeight = a;
                }
                match = Regex.Match(style, "width:(-?\\d+)px");
                if (match.Success)
                {
                    double a = 0;
                    double.TryParse(match.Groups[1].Value, out a);
                    se.PreviewWidth = a;
                }
                se.PreviewUrl = query.GetAttributeValue("data-src", "");
                if (string.IsNullOrWhiteSpace(se.PreviewUrl))
                    se.PreviewUrl = query.GetAttributeValue("src", "");
                res.Entries.Add(se);
            }
            return res;
        }
        public async Task<string> GatherImageLink(string pageurl)
        {
            var content = await client.GetAsync(pageurl);
            content.EnsureSuccessStatusCode();
            HtmlDocument doc = new();
            doc.Load(await content.Content.ReadAsStreamAsync());
            var imgnode = doc.DocumentNode.SelectSingleNode("//body//div[1]//div[2]//a//img");
            if (imgnode == null)
                throw new Exception("Unable to gather h@h image link.");

            return imgnode.GetAttributes().First(x => x.Name == "src").Value;
        }
        public struct GalleryInfo
        {
            public string Title;
            public string RawTitle;
            public Dictionary<string, List<string>> Tags;
            public Dictionary<string, string> Details;
            public double Rating;
            public int RateCount;
            public string Publisher;
        }
        public async Task<GalleryInfo> GetGalleryInfo(string catalogurl)
        {
            bool reload = false;
        rel:
            var content = await client.GetAsync(catalogurl + (reload ? "?nw=session" : ""));
            content.EnsureSuccessStatusCode();
            HtmlDocument doc = new();
            doc.Load(await content.Content.ReadAsStreamAsync());
            var details = doc.GetElementbyId("gdd")?.SelectNodes(".//table//tr");
            if (details == null || details.Count <= 1)
            {
                reload = true;
                goto rel;
            }
            GalleryInfo gi = default;
            gi.Details = new();
            foreach (var item in details)
            {
                if (item.ChildNodes.Count <= 1)
                {
                    continue;
                }
                var item0 = item.ChildNodes[0].InnerText;
                var item1 = item.ChildNodes[1].InnerText;
                if (item0 == "Language:")
                {
                    var items = item1.Split(" ");
                    if (items.Length > 1)
                    {
                        item1 = items[0] + "(翻译)";
                    }
                }
                item0 = item0 switch
                {
                    "Posted:" => "发布时间",
                    "Parent:" => "父画集",
                    "Visible:" => "是否公开",
                    "Language:" => "语言",
                    "File Size:" => "文件大小",
                    "Length:" => "页数",
                    "Favorited:" => "收藏数",
                    _ => item0.Substring(0, item0.Length - 1),
                };
                gi.Details.Add(item0, item1);
            }
            gi.Title = doc.GetElementbyId("gn")?.InnerText;
            gi.RawTitle = doc.GetElementbyId("gj")?.InnerText;
            var ratinglabel = (doc.GetElementbyId("rating_label")?.InnerText ?? "").Split(":");
            if (ratinglabel.Length > 1)
            {
                double rating = 0;
                double.TryParse(ratinglabel[1], out rating);
                gi.Rating = rating;
            }
            int ratecount = 0;
            int.TryParse(doc.GetElementbyId("rating_count")?.InnerText, out ratecount);
            gi.RateCount = ratecount;
            gi.Tags = new();
            var query = doc.GetElementbyId("taglist")?.SelectSingleNode(".//table");
            if (query != null)
            {
                foreach (var item in query.ChildNodes)
                {
                    var key = item.SelectSingleNode(".//td[1]")?.InnerText;
                    if (key == null)
                        continue;
                    var query2 = item.SelectSingleNode(".//td[2]")?.ChildNodes;
                    if (query2 == null)
                        continue;
                    List<string> cache = new();
                    foreach (var item2 in query2)
                    {
                        var query3 = item2.SelectSingleNode(".//a");
                        if (query3 == null)
                            continue;
                        if (!string.IsNullOrWhiteSpace(query3.InnerText))
                            cache.Add(query3.InnerText);
                    }
                    if (cache.Count > 0)
                    {
                        gi.Tags.Add(key, cache);
                    }
                }
            }
            gi.Publisher = doc.GetElementbyId("gdn")?.SelectSingleNode(".//a")?.InnerText;
            return gi;
        }
        public struct GalleryImage
        {
            public string PageUrl { get; set; }
            public string Title { get; set; }
            public bool IsLarge { get; set; }
            public ImageSource Preview { get; set; }
        }
        public async IAsyncEnumerable<GalleryImage> GetImages(string catalogurl)
        {
            bool reload = false;
        rel:
            var content = await client.GetAsync(catalogurl + (reload ? "?nw=session" : ""));
            content.EnsureSuccessStatusCode();
            HtmlDocument doc = new();
            doc.Load(await content.Content.ReadAsStreamAsync());
            var body = doc.DocumentNode.SelectSingleNode(".//html//body");
            var navi = body.ChildNodes.FirstOrDefault(x => x.HasClass("gtb"))?.SelectSingleNode(".//table//tr");
            if (navi == null)
            {
                reload = true;
                goto rel;
            }
            await foreach (var it in GetImagesDocumentAsync(doc))
                yield return it;
            var c = navi.ChildNodes.Count - 2;
            if (c < 1)
                throw new Exception("寄了");
            for (int i = 1; i < c; i++)
            {
                content = await client.GetAsync(catalogurl + "?p=" + i);
                content.EnsureSuccessStatusCode();
                doc = new();
                doc.Load(await content.Content.ReadAsStreamAsync());
                await foreach (var it in GetImagesDocumentAsync(doc))
                    yield return it;
            }
        }
        public async IAsyncEnumerable<GalleryImage> GetImagesDocumentAsync(HtmlDocument doc)
        {
            var catalogdiv = doc.GetElementbyId("gdt");
            HttpClient h = new();
            Dictionary<Uri, IBitmapFrame> caches = new();
            foreach (var item in catalogdiv.ChildNodes)
            {
                HtmlNode? div = item;
                div = div.FirstChild;
                if (div == null)
                    continue;
                GalleryImage gi = default;
                if (div.Name == "div")
                {
                    // Atlas mode
                    // div now has style like this:
                    // margin:1px auto 0; width:100px; height:144px; background:transparent url(https://example.com/path/to/atlas) -100px 0 no-repeat
                    var style = div.GetAttributeValue("style", "");
                    var match = Regex.Match(style,
                                            @"width:(\d+px);(\s)?height:(\d+px);(\s)?background:transparent url\((.+?)\) (-?\d+(px)?) (-?\d+(px)?) .+");
                    if (match.Success)
                    {
                        var width = int.Parse(match.Groups[1].Value.Replace("px", ""));
                        var height = int.Parse(match.Groups[2+1].Value.Replace("px", ""));
                        var imageUrl = match.Groups[3+2].Value;
                        var xOffset = int.Parse(match.Groups[4+2].Value.Replace("px", ""));
                        var yOffset = int.Parse(match.Groups[6+2].Value.Replace("px", ""));

                        xOffset = xOffset < 0 ? -xOffset : xOffset;
                        yOffset = yOffset < 0 ? -yOffset : yOffset;
                        var uri = new Uri(imageUrl);
                        IBitmapFrame? atlas;
                        caches.TryGetValue(uri, out atlas);
                        if (atlas == null)
                        {
                            var stream = await h.GetStreamAsync(uri);
                            MemoryStream ms = new();
                            await stream.CopyToAsync(ms);
                            atlas = caches[uri] = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());
                        }
                        gi.Preview = await LoadCroppedImage(atlas, new BitmapBounds { X = (uint)xOffset, Y = (uint)yOffset, Width = (uint)(width - 1), Height = (uint)(height - 1) });
                    }
                    // div = div.FirstChild;
                }
                else
                {
                    // Large mode(single image)
                    gi.Preview = new BitmapImage(new Uri(div.FirstChild.GetAttributes().First(x => x.Name == "src").Value));
                }
                gi.PageUrl = item.GetAttributes().First(x => x.Name == "href").Value;
                gi.Title = item.FirstChild.GetAttributes().First(x => x.Name == "title").Value;
                yield return gi;
            }
        }
        private async Task<ImageSource> LoadCroppedImage(IBitmapFrame frame, BitmapBounds Bounds)
        {
            var transform = new BitmapTransform
            {
                Bounds = Bounds
            };
            var pixelData = await frame.GetPixelDataAsync(
BitmapPixelFormat.Bgra8,
BitmapAlphaMode.Premultiplied,
transform,
ExifOrientationMode.IgnoreExifOrientation,
ColorManagementMode.DoNotColorManage);
            var writeableBitmap = new WriteableBitmap((int)Bounds.Width, (int)Bounds.Height);
            pixelData.DetachPixelData().CopyTo(writeableBitmap.PixelBuffer);
            return writeableBitmap;
        }
    }
}
