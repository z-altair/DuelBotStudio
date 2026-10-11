using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace DuelBotStudio.Services
{
    /// <summary>
    /// Downloads card images and caches them on disk and in a bounded in-memory LRU cache.
    /// </summary>
    public sealed class CardImageCache
    {
        private const int MaxMemoryCacheCapacity = 100;
        private readonly string _cacheDirectory;
        private readonly Dictionary<int, BitmapImage> _memoryCache = new();
        private readonly LinkedList<int> _lruList = new();
        private readonly Dictionary<int, Lazy<Task<BitmapImage>>> _inflightDownloads = new();
        private readonly HttpClient _httpClient;
        private readonly object _cacheLock = new();
        private readonly object _inflightLock = new();

        /// <summary>Creates an image cache using the per-user application cache directory.</summary>
        public CardImageCache()
            : this(null, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DuelBotStudio",
                "Cache",
                "Images"))
        {
        }

        /// <summary>Creates an image cache with optional HTTP and storage dependencies.</summary>
        /// <param name="httpClient">Optional HTTP client, useful for hosting and testing.</param>
        /// <param name="cacheDirectory">Writable directory used for cached images.</param>
        public CardImageCache(HttpClient? httpClient, string cacheDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cacheDirectory);
            _cacheDirectory = cacheDirectory;
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>Gets a card image from memory, disk, or the image service.</summary>
        /// <param name="cardId">Positive card database identifier.</param>
        /// <returns>The decoded card image.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cardId"/> is not positive.</exception>
        /// <exception cref="HttpRequestException">The image service could not be reached.</exception>
        /// <exception cref="IOException">The cache could not be read or written.</exception>
        public async Task<BitmapImage> GetCardImageAsync(int cardId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cardId);
            if (TryGetCachedImage(cardId, out var cachedImage))
            {
                return cachedImage;
            }

            Lazy<Task<BitmapImage>> download;
            lock (_inflightLock)
            {
                if (!_inflightDownloads.TryGetValue(cardId, out download!))
                {
                    download = new Lazy<Task<BitmapImage>>(
                        () => LoadCardImageAsync(cardId),
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    _inflightDownloads.Add(cardId, download);
                }
            }

            try
            {
                return await download.Value;
            }
            finally
            {
                if (download.IsValueCreated && download.Value.IsCompleted)
                {
                    lock (_inflightLock)
                    {
                        if (_inflightDownloads.TryGetValue(cardId, out var current) &&
                            ReferenceEquals(current, download))
                        {
                            _inflightDownloads.Remove(cardId);
                        }
                    }
                }
            }
        }

        private async Task<BitmapImage> LoadCardImageAsync(int cardId)
        {
            if (TryGetCachedImage(cardId, out var cachedImage))
            {
                return cachedImage;
            }

            Directory.CreateDirectory(_cacheDirectory);
            string filePath = Path.Combine(_cacheDirectory, $"{cardId}.jpg");
            BitmapImage? bitmap = File.Exists(filePath) ? LoadBitmapFromFile(filePath) : null;

            if (bitmap is null)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                byte[] imageBytes;
                try
                {
                    imageBytes = await _httpClient.GetByteArrayAsync(
                        $"https://images.ygoprodeck.com/images/cards/{cardId}.jpg");
                }
                catch (OperationCanceledException exception)
                {
                    throw new HttpRequestException("The card image request timed out.", exception);
                }
                string temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
                try
                {
                    await File.WriteAllBytesAsync(temporaryPath, imageBytes);
                    File.Move(temporaryPath, filePath, true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }

                bitmap = LoadBitmapFromFile(filePath);
                if (bitmap is null)
                {
                    File.Delete(filePath);
                    throw new InvalidDataException($"The image service returned invalid image data for card {cardId}.");
                }
            }

            AddToMemoryCache(cardId, bitmap);
            return bitmap;
        }

        private bool TryGetCachedImage(int cardId, out BitmapImage bitmap)
        {
            lock (_cacheLock)
            {
                if (_memoryCache.TryGetValue(cardId, out bitmap!))
                {
                    _lruList.Remove(cardId);
                    _lruList.AddLast(cardId);
                    return true;
                }
            }

            bitmap = null!;
            return false;
        }

        private void AddToMemoryCache(int cardId, BitmapImage bitmap)
        {
            lock (_cacheLock)
            {
                if (_memoryCache.ContainsKey(cardId))
                {
                    _lruList.Remove(cardId);
                }
                else if (_memoryCache.Count >= MaxMemoryCacheCapacity && _lruList.First is not null)
                {
                    _memoryCache.Remove(_lruList.First.Value);
                    _lruList.RemoveFirst();
                }

                _memoryCache[cardId] = bitmap;
                _lruList.AddLast(cardId);
            }
        }

        private static BitmapImage? LoadBitmapFromFile(string filePath)
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
            catch (FileFormatException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }
    }
}
