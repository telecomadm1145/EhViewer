using HtmlAgilityPack;
using System;
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
            res.StartUrl = WebUtility.HtmlDecode(nav.SelectSingleNode(".//div[2]//a")?.GetAttributeValue("href", null));
            res.PrevUrl = WebUtility.HtmlDecode(nav.SelectSingleNode(".//div[3]//a")?.GetAttributeValue("href", null));
            res.NextUrl = WebUtility.HtmlDecode(nav.SelectSingleNode(".//div[5]//a")?.GetAttributeValue("href", null));
            res.LastUrl = WebUtility.HtmlDecode(nav.SelectSingleNode(".//div[6]//a")?.GetAttributeValue("href", null));
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
        public struct GalleryImage
        {
            public string PageUrl { get; set; }
            public string Title { get; set; }
            public bool IsLarge { get; set; }
            public ImageSource Preview { get; set; }
        }
        public async IAsyncEnumerable<GalleryImage> GetImages(string catalogurl)
        {
            var content = await client.GetAsync(catalogurl);
            content.EnsureSuccessStatusCode();
            HtmlDocument doc = new();
            doc.Load(await content.Content.ReadAsStreamAsync());
            var body = doc.DocumentNode.SelectSingleNode(".//html//body");
            var navi = body.ChildNodes.First(x => x.HasClass("gtb")).SelectSingleNode(".//table//tr");
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
                                            @"width:(\d+px); height:(\d+px); background:transparent url\((.+?)\) (-?\d+(px)?) (-?\d+(px)?)");
                    if (match.Success)
                    {
                        var width = int.Parse(match.Groups[1].Value.Replace("px", ""));
                        var height = int.Parse(match.Groups[2].Value.Replace("px", ""));
                        var imageUrl = match.Groups[3].Value;
                        var xOffset = int.Parse(match.Groups[4].Value.Replace("px", ""));
                        var yOffset = int.Parse(match.Groups[6].Value.Replace("px", ""));

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
                    div = div.FirstChild;
                }
                else
                {
                    // Large mode(single image)
                    gi.Preview = new BitmapImage(new Uri(div.FirstChild.GetAttributes().First(x => x.Name == "src").Value));
                }
                gi.PageUrl = div.GetAttributes().First(x => x.Name == "href").Value;
                gi.Title = div.FirstChild.GetAttributes().First(x => x.Name == "title").Value;
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
