using DuelBotStudio.Models.Card;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DuelBotStudio.Services
{
    public class YgoCardInfoService
    {
        private static readonly string _dataCacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "Data");
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<int, CardModel> _memoryCache;

        public YgoCardInfoService()
        {
            if (!Directory.Exists(_dataCacheDirectory))
            {
                Directory.CreateDirectory(_dataCacheDirectory);
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://db.ygoprodeck.com/api/v7/")
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _memoryCache = new Dictionary<int, CardModel>();

        }

        public async Task<CardModel?> getCardByIdAsync(int cardId)
        {
            // Level 1: RAM Memory Check
            if (_memoryCache.TryGetValue(cardId, out var cachedCard))
            {
                return cachedCard;
            }

            string filePath = Path.Combine(_dataCacheDirectory, $"{cardId}.json");

            if (File.Exists(filePath))
            {
                try
                {
                    string jsonFromDisk = await File.ReadAllTextAsync(filePath);
                    CardModel? card = JsonSerializer.Deserialize<CardModel>(jsonFromDisk, _jsonOptions);
                    if (card != null)
                    {
                        _memoryCache[cardId] = card;
                        return card;
                    }
                }
                catch (Exception)
                {
                }
            }

            // Level 3: Fetch from Web API
            try
            {
                string jsonResponse = await _httpClient.GetStringAsync($"cardinfo.php?id={cardId}");
                var apiResponse = JsonSerializer.Deserialize<YgoApiResponseDto>(jsonResponse, _jsonOptions);

                var cardDto = apiResponse?.data?.FirstOrDefault();
                if (cardDto == null)
                {
                    return null;
                }

                var card = new CardModel
                {
                    id = cardDto.id,
                    name = cardDto.name ?? string.Empty,
                    description = cardDto.description ?? string.Empty,
                    type = cardDto.type ?? string.Empty,
                    attribute = cardDto.attribute ?? string.Empty,
                    race = cardDto.race ?? string.Empty,
                    atk = cardDto.atk,
                    def = cardDto.def,
                    level = cardDto.level,
                    imageUrl = cardDto.cardImages?.FirstOrDefault()?.imageUrl ?? string.Empty
                };

                string jsonToSave = JsonSerializer.Serialize(card, _jsonOptions);
                await File.WriteAllTextAsync(filePath, jsonToSave);

                _memoryCache[cardId] = card;
                return card;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal class YgoApiResponseDto
        {
            [JsonPropertyName("data")]
            public List<YgoCardDto> data { get; set; } = new List<YgoCardDto>();
        }

        internal class YgoCardDto
        {
            [JsonPropertyName("id")]
            public int id { get; set; }
            [JsonPropertyName("name")]
            public string? name { get; set; }
            [JsonPropertyName("type")]
            public string? type { get; set; }
            [JsonPropertyName("desc")]
            public string? description { get; set; }
            [JsonPropertyName("atk")]
            public int? atk { get; set; }
            [JsonPropertyName("def")]
            public int? def { get; set; }
            [JsonPropertyName("level")]
            public int? level { get; set; }

            [JsonPropertyName("race")]
            public string? race { get; set; }

            [JsonPropertyName("attribute")]
            public string? attribute { get; set; }

            [JsonPropertyName("card_images")]
            public List<YgoCardImageDto> cardImages { get; set; } = new List<YgoCardImageDto>();
        }

        internal class YgoCardImageDto
        {
            [JsonPropertyName("image_url")]
            public string? imageUrl { get; set; }
        }

        public async Task<List<CardModel>> searchCardsByNameAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                return new List<CardModel>();
            }

            string encodedQuery = Uri.EscapeDataString(query);

            Task<string?> nameTask = fetchJsonSafeAsync($"cardinfo.php?fname={encodedQuery}");
            Task<string?> descTask = fetchJsonSafeAsync($"cardinfo.php?desc={encodedQuery}");

            await Task.WhenAll(nameTask, descTask);

            List<CardModel> nameResults = parseCardsFromJson(nameTask.Result ?? string.Empty);
            List<CardModel> descResults = parseCardsFromJson(descTask.Result ?? string.Empty);

            return nameResults
                .UnionBy(descResults, card => card.id)
                .ToList();
        }

        private async Task<string?> fetchJsonSafeAsync(string endpoint)
        {
            try
            {
                return await _httpClient.GetStringAsync(endpoint);
            }
            catch
            {
                return null;
            }
        }

        private List<CardModel> parseCardsFromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<CardModel>();
            }

            try
            {
                var apiResponse = JsonSerializer.Deserialize<YgoApiResponseDto>(json, _jsonOptions);
                return apiResponse?.data?.Select(dto => new CardModel
                {
                    id = dto.id,
                    name = dto.name ?? string.Empty,
                    type = dto.type ?? string.Empty,
                    description = dto.description ?? string.Empty,
                    atk = dto.atk,
                    def = dto.def,
                    level = dto.level,
                    race = dto.race ?? string.Empty,
                    attribute = dto.attribute ?? string.Empty,
                    imageUrl = dto.cardImages.FirstOrDefault()?.imageUrl ?? string.Empty
                }).ToList() ?? new List<CardModel>();
            }
            catch
            {
                return new List<CardModel>();
            }
        }

    }
}
