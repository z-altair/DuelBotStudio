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

            var errors = await viewModel.LoadDeckAsync(path);

            Assert.IsNotEmpty(errors);
            Assert.AreSame(originalDeck, viewModel.DeckManager.CurrentDeck);
            Assert.AreEqual(99, viewModel.DeckManager.CurrentDeck.MainDeck[0].Id);
        }
        finally
        {
            File.Delete(path);
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
