using System.IO;
using System.Net;
using System.Net.Http;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class MainWindowViewModelTests
{
    [TestMethod]
    public async Task LoadDeckAsync_InvalidFilePreservesCurrentDeck()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydk");
        await File.WriteAllTextAsync(path, "#main\n42\nnot-a-card\n");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        try
        {
            var viewModel = new MainWindowViewModel(
                new YgoCardInfoService(httpClient, Path.GetTempPath()),
                new YdkParserService());
            viewModel.DeckManager.AddCard(new CardModel { Id = 99 }, DeckSection.Main);
            var originalDeck = viewModel.DeckManager.CurrentDeck;
            Assert.IsTrue(viewModel.IsDirty);

            var errors = await viewModel.LoadDeckAsync(path);

            Assert.IsNotEmpty(errors);
            Assert.AreSame(originalDeck, viewModel.DeckManager.CurrentDeck);
            Assert.AreEqual(99, viewModel.DeckManager.CurrentDeck.MainDeck[0].Id);
            Assert.IsTrue(viewModel.IsDirty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task SaveDeckAsync_WritesDeckAndClearsDirtyState()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"DuelBotStudio.Save-{Guid.NewGuid():N}");
        string filePath = Path.Combine(directory, "deck.ydk");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var viewModel = new MainWindowViewModel(
            new YgoCardInfoService(httpClient, Path.Combine(directory, "cache")),
            new YdkParserService());
        viewModel.DeckManager.AddCard(new CardModel { Id = 42 }, DeckSection.Main);

        try
        {
            await viewModel.SaveDeckAsync(filePath);

            Assert.IsFalse(viewModel.IsDirty);
            Assert.AreEqual(filePath, viewModel.DeckManager.CurrentDeck.FilePath);
            var parsed = new YdkParserService().ParseFile(filePath);
            Assert.IsFalse(parsed.HasErrors);
            CollectionAssert.AreEqual(new[] { 42 }, parsed.MainDeckIds);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
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
