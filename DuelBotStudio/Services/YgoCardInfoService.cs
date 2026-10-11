using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DuelBotStudio.Models.Card;

namespace DuelBotStudio.Services
{
    /// <summary>
    /// Retrieves Yu-Gi-Oh! card data from YGOPRODeck and maintains a local JSON cache.
    /// </summary>
    public sealed class YgoCardInfoService
    {
        private const string ApiBaseAddress = "https://db.ygoprodeck.com/api/v7/";
        private readonly string _dataCacheDirectory;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ConcurrentDictionary<int, CardModel> _memoryCache = new();
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _requestLocks = new();

        /// <summary>Creates a card service using the per-user application cache directory.</summary>
        public YgoCardInfoService()
            : this(null, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DuelBotStudio",
                "Cache",
                "Data"))
        {
        }

        /// <summary>Creates a service with an optional HTTP client and cache directory.</summary>
        /// <param name="httpClient">Optional HTTP client, useful for hosting and testing.</param>
        /// <param name="dataCacheDirectory">Writable directory used for cached card data.</param>
        public YgoCardInfoService(HttpClient? httpClient, string dataCacheDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dataCacheDirectory);
            _dataCacheDirectory = dataCacheDirectory;
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.BaseAddress ??= new Uri(ApiBaseAddress);
        }

        /// <summary>Gets a card by ID, returning null only when the API reports no matching card.</summary>
        /// <param name="cardId">Positive card database identifier.</param>
        /// <returns>The requested card, or null if it is not present in the database.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cardId"/> is not positive.</exception>
        /// <exception cref="HttpRequestException">The card service request fails or times out.</exception>
        /// <exception cref="IOException">The local metadata cache cannot be read or written.</exception>
        /// <exception cref="JsonException">The card service returns invalid data.</exception>
        public async Task<CardModel?> GetCardByIdAsync(int cardId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cardId);

            if (_memoryCache.TryGetValue(cardId, out var cachedCard))
            {
                return cachedCard;
            }

            var requestLock = _requestLocks.GetOrAdd(cardId, _ => new SemaphoreSlim(1, 1));
            await requestLock.WaitAsync();
            try
            {
                if (_memoryCache.TryGetValue(cardId, out cachedCard))
                {
                    return cachedCard;
                }

                return await LoadCardByIdAsync(cardId);
            }
            finally
            {
                requestLock.Release();
            }
        }

        private async Task<CardModel?> LoadCardByIdAsync(int cardId)
        {
            Directory.CreateDirectory(_dataCacheDirectory);
            string filePath = Path.Combine(_dataCacheDirectory, $"{cardId}.json");
            if (File.Exists(filePath))
            {
                string cachedJson = await File.ReadAllTextAsync(filePath);
                CardModel? cachedCard = null;
                try
                {
                    cachedCard = JsonSerializer.Deserialize<CardModel>(cachedJson, _jsonOptions);
                }
                catch (JsonException)
                {
                    Trace.TraceWarning("Discarding malformed card metadata cache entry: {0}", filePath);
                }

                if (cachedCard?.Id == cardId)
                {
                    _memoryCache.TryAdd(cardId, cachedCard);
                    return cachedCard;
                }

                File.Delete(filePath);
            }

            string? jsonResponse = await GetApiResponseAsync($"cardinfo.php?id={cardId}");
            if (jsonResponse is null)
            {
                return null;
            }

            var apiResponse = JsonSerializer.Deserialize<YgoApiResponseDto>(jsonResponse, _jsonOptions)
                ?? throw new JsonException("The card API returned an empty response.");
            var cards = apiResponse.Data
                ?? throw new JsonException("The card API response is missing its data array.");
            var cardDto = cards.FirstOrDefault();
            if (cardDto is null)
            {
                return null;
            }

            if (cardDto.Id != cardId)
            {
                throw new JsonException($"The card API returned ID {cardDto.Id} for requested ID {cardId}.");
            }

            var result = ToCardModel(cardDto);
            string temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(result, _jsonOptions));
                File.Move(temporaryPath, filePath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            _memoryCache.TryAdd(cardId, result);
            return result;
        }

        /// <summary>Searches card names and descriptions, deduplicating results by card ID.</summary>
        /// <param name="query">Search text; values shorter than three characters return no results.</param>
        /// <returns>Matching cards, deduplicated by identifier.</returns>
        /// <exception cref="HttpRequestException">A search request fails or times out.</exception>
        /// <exception cref="JsonException">The card service returns invalid data.</exception>
        public async Task<List<CardModel>> SearchCardsByNameAsync(string query)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
            {
                return new List<CardModel>();
            }

            string encodedQuery = Uri.EscapeDataString(query.Trim());
            Task<string?> nameTask = GetApiResponseAsync($"cardinfo.php?fname={encodedQuery}");
            Task<string?> descriptionTask = GetApiResponseAsync($"cardinfo.php?desc={encodedQuery}");
            await Task.WhenAll(nameTask, descriptionTask);

            var nameResults = ParseCardsFromJson(nameTask.Result);
            var descriptionResults = ParseCardsFromJson(descriptionTask.Result);
            return nameResults.UnionBy(descriptionResults, card => card.Id).ToList();
        }

        private async Task<string?> GetApiResponseAsync(string endpoint)
        {
            try
            {
                using var response = await _httpClient.GetAsync(endpoint);
                string body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.BadRequest &&
                    body.Contains("No card matching your query", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return body;
            }
            catch (OperationCanceledException exception)
            {
                throw new HttpRequestException("The card service request timed out.", exception);
            }
        }

        private List<CardModel> ParseCardsFromJson(string? json)
        {
            if (json is null)
            {
                return new List<CardModel>();
            }

            var apiResponse = JsonSerializer.Deserialize<YgoApiResponseDto>(json, _jsonOptions)
                ?? throw new JsonException("The card API returned an empty response.");
            var cards = apiResponse.Data
                ?? throw new JsonException("The card API response is missing its data array.");
            return cards.Select(ToCardModel).ToList();
        }

        private static CardModel ToCardModel(YgoCardDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            Description = dto.Description ?? string.Empty,
            Type = dto.Type ?? string.Empty,
            Attribute = dto.Attribute ?? string.Empty,
            Race = dto.Race ?? string.Empty,
            Atk = dto.Atk,
            Def = dto.Def,
            Level = dto.Level,
            ImageUrl = dto.CardImages?.FirstOrDefault()?.ImageUrl ?? string.Empty
        };

        private sealed class YgoApiResponseDto
        {
            [JsonPropertyName("data")]
            public List<YgoCardDto>? Data { get; set; } = new();
        }

        private sealed class YgoCardDto
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("desc")]
            public string? Description { get; set; }
            [JsonPropertyName("atk")]
            public int? Atk { get; set; }
            [JsonPropertyName("def")]
            public int? Def { get; set; }
            [JsonPropertyName("level")]
            public int? Level { get; set; }
            [JsonPropertyName("race")]
            public string? Race { get; set; }
            [JsonPropertyName("attribute")]
            public string? Attribute { get; set; }
            [JsonPropertyName("card_images")]
            public List<YgoCardImageDto>? CardImages { get; set; } = new();
        }

        private sealed class YgoCardImageDto
        {
            [JsonPropertyName("image_url")]
            public string? ImageUrl { get; set; }
        }
    }
}
