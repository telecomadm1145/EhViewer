using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;

namespace EhViewer
{
    internal class CachingService
    {
        public static CachingService Instance { get; set; } = new();
        private static SHA256 sha256Hash = SHA256.Create();
        private const string CACHE_FOLDER = "ImageCache";

        private CachingService()
        {
        }

        public async Task<byte[]?> GetFromCache(string id)
        {
            id = ComputeSha256Hash(id);
            try
            {
                var cacheFolder = await GetCacheFolder();
                var file = await cacheFolder.TryGetItemAsync(id) as StorageFile;

                if (file != null)
                {
                    using (var stream = await file.OpenStreamForReadAsync())
                    {
                        var bytes = new byte[stream.Length];
                        await stream.ReadAsync(bytes, 0, bytes.Length);
                        return bytes;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading from cache: {ex.Message}");
            }

            return null;
        }

        public async Task SaveToCache(string id, byte[] imageData)
        {
            id = ComputeSha256Hash(id);
            try
            {
                var cacheFolder = await GetCacheFolder();
                var file = await cacheFolder.CreateFileAsync(id, CreationCollisionOption.ReplaceExisting);

                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    await stream.WriteAsync(imageData, 0, imageData.Length);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving to cache: {ex.Message}");
            }
        }

        private async Task<StorageFolder> GetCacheFolder()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var cacheFolder = await localFolder.CreateFolderAsync(CACHE_FOLDER, CreationCollisionOption.OpenIfExists);
            return cacheFolder;
        }
        public static string ComputeSha256Hash(string rawData)
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        public async Task ClearCache()
        {
            try
            {
                var cacheFolder = await GetCacheFolder();
                await cacheFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing cache: {ex.Message}");
            }
        }
    }

    public class OfflineDb
    {
        private readonly string dbFileName = "offline_gallery.json";
        private readonly SemaphoreSlim syncLock = new SemaphoreSlim(1, 1);

        // 使用ConcurrentBag替代List确保线程安全
        private List<OfflineGalleryInfo> ogis = new();
        public IReadOnlyList<OfflineGalleryInfo> Infos => ogis;

        private bool loaded = false;

        public OfflineDb()
        {
        }
        public static OfflineDb Instance = new();
        public async Task Load()
        {
            if (loaded)
                return;
            loaded = true;
            try
            {
                await syncLock.WaitAsync();
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.TryGetItemAsync(dbFileName) as StorageFile;

                if (file != null)
                {
                    var json = await FileIO.ReadTextAsync(file);
                    var infos = JsonSerializer.Deserialize<List<OfflineGalleryInfo>>(json);
                    ogis = new(infos ?? new List<OfflineGalleryInfo>());
                }
            }
            finally
            {
                syncLock.Release();
            }
        }

        public bool OfflineMode { get; set; } = false;

        public async Task Sync()
        {
            try
            {
                await syncLock.WaitAsync();
                await SyncNoLock();
            }
            finally
            {
                syncLock.Release();
            }
        }

        private async Task SyncNoLock()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync(dbFileName, CreationCollisionOption.ReplaceExisting);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(Infos.ToList(), options);
            await FileIO.WriteTextAsync(file, json);
        }

        // 添加新的画廊信息
        public async Task AddGalleryInfo(OfflineGalleryInfo info)
        {
            try
            {
                await syncLock.WaitAsync();
                ogis.Add(info);
                await SyncNoLock();
            }
            finally
            {
                syncLock.Release();
            }
        }

        // 移除画廊信息
        public async Task RemoveGalleryInfo(string url)
        {
            try
            {
                await syncLock.WaitAsync();
                ogis.Remove(ogis.FirstOrDefault(x => new Uri(x.Url).AbsolutePath == new Uri(url).AbsolutePath));
                await SyncNoLock();
            }
            finally
            {
                syncLock.Release();
            }
        }

        // 更新画廊信息
        public async Task UpdateGalleryInfo(OfflineGalleryInfo info)
        {
            await RemoveGalleryInfo(info.Url);
            await AddGalleryInfo(info);
        }
    }

    // 将struct改为class以支持更好的序列化
    public class OfflineGalleryInfo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("gallery_info")]
        public EhApi.GalleryInfo GalleryInfo { get; set; }

        [JsonPropertyName("images")]
        public List<string> Images { get; set; } = new();
    }
}