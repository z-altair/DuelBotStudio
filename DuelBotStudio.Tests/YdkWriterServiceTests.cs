using System.IO;
using System.Net;
using System.Net.Http;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;
using DuelBotStudio.Services;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class YdkWriterServiceTests
{
    [TestMethod]
    public async Task WriteFileAsync_WritesAllDeckSectionsInOrder()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydk");
        using var client = new HttpClient(new StubHttpMessageHandler());
        var manager = new DeckManager(new YgoCardInfoService(client, Path.GetTempPath()));
        manager.AddCard(new CardModel { Id = 10 }, DeckSection.Main);
        manager.AddCard(new CardModel { Id = 11 }, DeckSection.Main);
        manager.AddCard(new CardModel { Id = 20 }, DeckSection.Extra);
        manager.AddCard(new CardModel { Id = 30 }, DeckSection.Side);

        try
        {
            await new YdkWriterService().WriteFileAsync(manager.CurrentDeck, filePath);

            var result = new YdkParserService().ParseFile(filePath);
            Assert.IsFalse(result.HasErrors);
            CollectionAssert.AreEqual(new[] { 10, 11 }, result.MainDeckIds);
            CollectionAssert.AreEqual(new[] { 20 }, result.ExtraDeckIds);
            CollectionAssert.AreEqual(new[] { 30 }, result.SideDeckIds);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [TestMethod]
    public async Task WriteFileAsync_RejectsInvalidCardIdsWithoutReplacingDestination()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydk");
        await File.WriteAllTextAsync(filePath, "existing");
        using var client = new HttpClient(new StubHttpMessageHandler());
        var manager = new DeckManager(new YgoCardInfoService(client, Path.GetTempPath()));
        manager.AddCard(new CardModel { Id = 0, Name = "Invalid" }, DeckSection.Main);

        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(
                () => new YdkWriterService().WriteFileAsync(manager.CurrentDeck, filePath));

            Assert.AreEqual("existing", await File.ReadAllTextAsync(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
