using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EhViewer
{
    public class EhTag
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("intro")]
        public string Description { get; set; }
        [JsonPropertyName("links")]
        public string Links { get; set; }
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
        public Dictionary<string, object> Data { get; set; }
    }
    public class EhTagTranslation
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("data")]
        public List<EhMainTag> Data { get; set; }
    }
}
