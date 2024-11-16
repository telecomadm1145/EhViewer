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
}