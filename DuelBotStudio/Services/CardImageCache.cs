using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Media.Imaging;

namespace DuelBotStudio.Services
{
    public static class CardImageCache
    {
        private const int MAX_MEMORY_CACHE_CAPACITY = 100;
        private static readonly string _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "Images");
        private static readonly Dictionary<int, BitmapImage> _memoryCache = new Dictionary<int, BitmapImage>();
        private static readonly LinkedList<int> _lruList = new LinkedList<int>();
        private static readonly HttpClient _httpClient = new HttpClient();

        static CardImageCache()
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public static async Task<BitmapImage?> getCardImageAsync(int cardId)
        {
            // Level 1: RAM Memory Cache
            if (_memoryCache.TryGetValue(cardId, out var cachedImage))
            {
                _lruList.Remove(cardId);
                _lruList.AddLast(cardId);
                return cachedImage;
            }

            string filePath = Path.Combine(_cacheDirectory, $"{cardId}.jpg");

            // Level 2: Local Disk Cache
            if (!File.Exists(filePath))
            {
                try
                {
                    byte[] imageBytes = await _httpClient.GetByteArrayAsync($"https://images.ygoprodeck.com/images/cards/{cardId}.jpg");
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            // Level 3: Load from Disk into BitmapImage
            BitmapImage? bitmap = loadBitmapFromFile(filePath);
            if (bitmap != null)
            {
                addToMemoryCache(cardId, bitmap);
            }

            return bitmap;
        }

        private static void addToMemoryCache(int cardId, BitmapImage bitmap)
        {
            if (_memoryCache.Count >= MAX_MEMORY_CACHE_CAPACITY)
            {
                LinkedListNode<int>? oldestNode = _lruList.First;
                if (oldestNode != null)
                {
                    int oldestCardId = oldestNode.Value;
                    _lruList.RemoveFirst();
                    _memoryCache.Remove(oldestCardId);
                }
            }

            _memoryCache[cardId] = bitmap;
            _lruList.AddLast(cardId);
        }

        private static BitmapImage? loadBitmapFromFile(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
