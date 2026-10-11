using System.IO;
using System.Net;
using System.Net.Http;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class DeckEditorViewModelTests
{
    [TestMethod]
    public void ApplyRules_UpdatesDeckLimitsAndValidation()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var cardInfoService = new YgoCardInfoService(httpClient, Path.GetTempPath());
        var manager = new DeckManager(cardInfoService);
        var viewModel = new DeckEditorViewModel(cardInfoService, manager)
        {
            MinimumMainDeckSize = "0",
            MaximumMainDeckSize = "80",
            MaximumExtraDeckSize = "20",
            MaximumSideDeckSize = "20",
            MaximumCopiesPerCard = "4"
        };

        viewModel.ApplyRules();

        Assert.AreEqual(0, manager.Rules.MinimumMainDeckSize);
        Assert.AreEqual(80, manager.Rules.MaximumMainDeckSize);
        Assert.AreEqual(20, manager.Rules.MaximumExtraDeckSize);
        Assert.AreEqual(20, manager.Rules.MaximumSideDeckSize);
        Assert.AreEqual(4, manager.Rules.MaximumCopiesPerCard);
        Assert.IsTrue(manager.Validate().IsValid);
        Assert.AreEqual("Deck meets configured limits.", viewModel.ValidationStatus);
        Assert.AreEqual("Deck limits applied.", viewModel.StatusMessage);
    }

    [TestMethod]
    public void ApplyRules_RejectsInvalidLimitsWithoutChangingCurrentRules()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var cardInfoService = new YgoCardInfoService(httpClient, Path.GetTempPath());
        var manager = new DeckManager(cardInfoService);
        var originalRules = manager.Rules;
        var viewModel = new DeckEditorViewModel(cardInfoService, manager)
        {
            MinimumMainDeckSize = "50",
            MaximumMainDeckSize = "40"
        };

        viewModel.ApplyRules();

        Assert.AreSame(originalRules, manager.Rules);
        StringAssert.Contains(viewModel.StatusMessage, "invalid");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}
