using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EhViewer
{
    public class Data
    {
        [JsonPropertyName("raw")]
        public string Raw { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }
        [JsonPropertyName("Html")]
        public string Html { get; set; }
        [JsonPropertyName("Ast")]
        public object Ast { get; set; }
    }
    public class EhTag
    {
        [JsonPropertyName("name")]
        public Data Name { get; set; }
        [JsonPropertyName("intro")]
        public Data Description { get; set; }
        [JsonPropertyName("links")]
        public Data Links { get; set; }
    }
    public class EhMainTagInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("key")]
        public string Key { get; set; }
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        [JsonPropertyName("aliases")]
        public List<string> Alias { get; set; }
    }
    public class EhMainTag
    {
        [JsonPropertyName("namespace")]
        public string Name { get; set; }
        [JsonPropertyName("frontMatters")]
        public EhMainTagInfo Info { get; set; }
        [JsonPropertyName("data")]
        public Dictionary<string, EhTag> Data { get; set; }
    }
    public class EhTagTranslation
    {
        //[JsonPropertyName("version")]
        //public string Version { get; set; }
        [JsonPropertyName("data")]
        public List<EhMainTag> Data { get; set; }

        public static EhTagTranslation Instance = new();
        public async Task Update()
        {
            Debug.WriteLine("Updating EhTagTranslation");
            var hc = new HttpClient();
            if (await CachingService.Instance.GetFromCache("EhTagTranslation!Data") == null)
            {
                var resp = await hc.GetAsync($"https://github.com/EhTagTranslation/Database/releases/latest/download/db.full.json");
                resp.EnsureSuccessStatusCode();
                var tags = await resp.Content.ReadAsByteArrayAsync();
                await CachingService.Instance.SaveToCache("EhTagTranslation!Data", tags);
            }
            var dat = JsonSerializer.Deserialize<EhTagTranslation>(await CachingService.Instance.GetFromCache("EhTagTranslation!Data"));
            // this.Version = dat.Version;
            this.Data = dat.Data;
            Debug.WriteLine("Updated!");
        }
    }

}
