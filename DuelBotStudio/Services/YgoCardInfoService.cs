using DuelBotStudio.Models.Card;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

        public async Task<CardModel?> GetCardByIdAsync(int cardId)
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

                var cardDto = apiResponse?.Data?.FirstOrDefault();
                if (cardDto == null)
                {
                    return null;
                }

                var card = new CardModel
                {
                    Id = cardDto.Id,
                    Name = cardDto.Name ?? string.Empty,
                    Description = cardDto.Description ?? string.Empty,
                    Type = cardDto.Type ?? string.Empty,
                    Attribute = cardDto.Attribute ?? string.Empty,
                    Race = cardDto.Race ?? string.Empty,
                    Atk = cardDto.Atk,
                    Def = cardDto.Def,
                    Level = cardDto.Level,
                    ImageUrl = cardDto.CardImages?.FirstOrDefault()?.ImageUrl ?? string.Empty
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
            public List<YgoCardDto> Data { get; set; } = new List<YgoCardDto>();
        }

        internal class YgoCardDto
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
            public List<YgoCardImageDto> CardImages { get; set; } = new List<YgoCardImageDto>();
        }

        internal class YgoCardImageDto
        {
            [JsonPropertyName("image_url")]
            public string? ImageUrl { get; set; }
        }

        public async Task<List<CardModel>> SearchCardsByNameAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                return new List<CardModel>();
            }

            string encodedQuery = Uri.EscapeDataString(query);

            Task<string?> nameTask = FetchJsonSafeAsync($"cardinfo.php?fname={encodedQuery}");
            Task<string?> descTask = FetchJsonSafeAsync($"cardinfo.php?desc={encodedQuery}");

            await Task.WhenAll(nameTask, descTask);

            List<CardModel> nameResults = ParseCardsFromJson(nameTask.Result ?? string.Empty);
            List<CardModel> descResults = ParseCardsFromJson(descTask.Result ?? string.Empty);

            return nameResults
                .UnionBy(descResults, card => card.Id)
                .ToList();
        }

        private async Task<string?> FetchJsonSafeAsync(string endpoint)
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

        private List<CardModel> ParseCardsFromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<CardModel>();
            }

            try
            {
                var apiResponse = JsonSerializer.Deserialize<YgoApiResponseDto>(json, _jsonOptions);
                return apiResponse?.Data?.Select(dto => new CardModel
                {
                    Id = dto.Id,
                    Name = dto.Name ?? string.Empty,
                    Type = dto.Type ?? string.Empty,
                    Description = dto.Description ?? string.Empty,
                    Atk = dto.Atk,
                    Def = dto.Def,
                    Level = dto.Level,
                    Race = dto.Race ?? string.Empty,
                    Attribute = dto.Attribute ?? string.Empty,
                    ImageUrl = dto.CardImages.FirstOrDefault()?.ImageUrl ?? string.Empty
                }).ToList() ?? new List<CardModel>();
            }
            catch
            {
                return new List<CardModel>();
            }
        }

    }
}
