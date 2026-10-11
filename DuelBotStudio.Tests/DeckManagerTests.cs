using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using DuelBotStudio.Models;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;
using DuelBotStudio.Services;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class DeckManagerTests
{
    [TestMethod]
    public void AddCard_UsesExplicitDestinationSection()
    {
        var manager = CreateManager(new DeckRules(0, 60, 15, 15, 3));

        manager.AddCard(new CardModel { Id = 10, Name = "Card 10" }, DeckSection.Side);

        Assert.IsEmpty(manager.CurrentDeck.MainDeck);
        Assert.IsEmpty(manager.CurrentDeck.ExtraDeck);
        Assert.HasCount(1, manager.CurrentDeck.SideDeck);
        Assert.AreEqual(10, manager.CurrentDeck.SideDeck[0].Id);
    }

    [TestMethod]
    public void Validate_UsesConfiguredSectionAndCopyLimits()
    {
        var manager = CreateManager(new DeckRules(1, 2, 1, 1, 2));
        manager.AddCard(new CardModel { Id = 1 }, DeckSection.Main);
        manager.AddCard(new CardModel { Id = 1 }, DeckSection.Main);
        manager.AddCard(new CardModel { Id = 1 }, DeckSection.Main);
        manager.AddCard(new CardModel { Id = 1 }, DeckSection.Side);

        var result = manager.Validate();

        Assert.IsFalse(result.IsValid);
        Assert.HasCount(2, result.Errors);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("Main deck")));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("copies")));
    }

    [TestMethod]
    public void Validate_AcceptsDeckWithinConfiguredLimits()
    {
        var manager = CreateManager(new DeckRules(2, 2, 1, 1, 3));
        manager.AddCard(new CardModel { Id = 1 }, DeckSection.Main);
        manager.AddCard(new CardModel { Id = 2 }, DeckSection.Main);
        manager.AddCard(new CardModel { Id = 3 }, DeckSection.Extra);
        manager.AddCard(new CardModel { Id = 4 }, DeckSection.Side);

        var result = manager.Validate();

        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.Errors);
    }

    [TestMethod]
    public async Task LoadFromParseResultAsync_PreservesCurrentDeckWhenLookupFails()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        string cachePath = CreateTemporaryDirectory();
        try
        {
            var manager = new DeckManager(new YgoCardInfoService(httpClient, cachePath));
            manager.AddCard(new CardModel { Id = 99, Name = "Existing card" }, DeckSection.Main);
            var originalDeck = manager.CurrentDeck;
            var parseResult = new YdkParseResult();
            parseResult.MainDeckIds.Add(42);

            HttpRequestException? exception = null;
            try
            {
                await manager.LoadFromParseResultAsync(parseResult);
            }
            catch (HttpRequestException caught)
            {
                exception = caught;
            }

            Assert.IsNotNull(exception);
            Assert.AreSame(originalDeck, manager.CurrentDeck);
            Assert.AreEqual(99, manager.CurrentDeck.MainDeck[0].Id);
        }
        finally
        {
            Directory.Delete(cachePath, true);
        }
    }

    [TestMethod]
    public async Task LoadFromParseResultAsync_RejectsUnknownCardAndPreservesCurrentDeck()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"error":"No card matching your query was found in the database."}""",
                    Encoding.UTF8,
                    "application/json")
            }));
        string cachePath = CreateTemporaryDirectory();
        try
        {
            var manager = new DeckManager(new YgoCardInfoService(httpClient, cachePath));
            manager.AddCard(new CardModel { Id = 99, Name = "Existing card" }, DeckSection.Main);
            var originalDeck = manager.CurrentDeck;
            var parseResult = new YdkParseResult();
            parseResult.MainDeckIds.Add(999999);
            parseResult.CardIdLineNumbers.Add(999999, new List<int> { 1 });

            InvalidDataException? exception = null;
            try
            {
                await manager.LoadFromParseResultAsync(parseResult);
            }
            catch (InvalidDataException caught)
            {
                exception = caught;
            }

            Assert.IsNotNull(exception);
            StringAssert.Contains(exception.Message, "line(s) 1");
            Assert.AreSame(originalDeck, manager.CurrentDeck);
            Assert.AreEqual(99, manager.CurrentDeck.MainDeck[0].Id);
        }
        finally
        {
            Directory.Delete(cachePath, true);
        }
    }

    [TestMethod]
    public async Task GetCardByIdAsync_RepairsCorruptDiskCache()
    {
        const int cardId = 123;
        string cachePath = CreateTemporaryDirectory();
        string cachedCardPath = Path.Combine(cachePath, $"{cardId}.json");
        await File.WriteAllTextAsync(cachedCardPath, "{ invalid json");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":[{"id":123,"name":"Recovered Card","type":"Effect Monster","desc":"Text","card_images":[]}]}""",
                    Encoding.UTF8,
                    "application/json")
            }));

        try
        {
            var service = new YgoCardInfoService(httpClient, cachePath);

            var result = await service.GetCardByIdAsync(cardId);

            Assert.IsNotNull(result);
            Assert.AreEqual("Recovered Card", result.Name);
            StringAssert.Contains(await File.ReadAllTextAsync(cachedCardPath), "Recovered Card");
        }
        finally
        {
            Directory.Delete(cachePath, true);
        }
    }

    private static DeckManager CreateManager(DeckRules rules)
    {
        var client = new HttpClient(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        return new DeckManager(new YgoCardInfoService(client, Path.GetTempPath()), rules);
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"DuelBotStudio.Tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
