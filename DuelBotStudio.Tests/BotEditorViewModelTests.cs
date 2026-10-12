using System.IO;
using System.Net;
using System.Net.Http;
using DuelBotStudio.Models.Bot;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class BotEditorViewModelTests
{
    [TestMethod]
    public async Task ApplyRuleAndSaveProfile_PersistsCardConditionAndDefensiveTransition()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"DuelBotStudio.BotEditor-{Guid.NewGuid():N}");
        string filePath = Path.Combine(directory, "bot.json");
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var viewModel = new BotEditorViewModel(
            new YgoCardInfoService(httpClient, directory),
            new BotProfileJsonService());
        Directory.CreateDirectory(directory);

        viewModel.BeginNewRule();
        viewModel.Draft.Name = "Summon from hand";
        viewModel.Draft.DecisionPoint = BotDecisionPoint.MainPhaseCommand;
        viewModel.Draft.Action = BotActionType.NormalSummon;
        viewModel.Draft.Priority = "100";
        viewModel.Draft.TargetCardId = "89631139";
        viewModel.Draft.TargetCardName = "Blue-Eyes White Dragon";
        viewModel.Draft.NextModeId = "defensive";
        viewModel.Draft.AddCondition();
        viewModel.Draft.Conditions[0].CardId = 89631139;
        viewModel.Draft.Conditions[0].CardName = "Blue-Eyes White Dragon";
        viewModel.Draft.Conditions[0].Zone = BotCardZone.Hand;

        Assert.IsTrue(viewModel.ApplyRule());

        try
        {
            await viewModel.SaveProfileAsync(filePath);

            Assert.IsFalse(viewModel.IsDirty);
            BotProfile loaded = await new BotProfileJsonService().ReadFileAsync(filePath);
            Assert.AreEqual("defensive", loaded.Rules[0].NextModeId);
            Assert.AreEqual(BotCardZone.Hand, loaded.Rules[0].Conditions[0].Zone);
            Assert.AreEqual(100, loaded.Rules[0].Priority);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task LoadProfileAsync_InvalidJsonPreservesCurrentProfile()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(filePath, "{ invalid json");
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var viewModel = new BotEditorViewModel(
            new YgoCardInfoService(httpClient, Path.GetTempPath()),
            new BotProfileJsonService());
        viewModel.BotName = "Keep this profile";

        try
        {
            await Assert.ThrowsAsync<System.Text.Json.JsonException>(
                () => viewModel.LoadProfileAsync(filePath));

            Assert.AreEqual("Keep this profile", viewModel.BotName);
            Assert.IsTrue(viewModel.IsDirty);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task SaveProfileAsync_EmptyNewRuleDraftDoesNotCreateInvalidRule()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"DuelBotStudio.BotEditor-{Guid.NewGuid():N}");
        string filePath = Path.Combine(directory, "empty-profile.json");
        Directory.CreateDirectory(directory);
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var viewModel = new BotEditorViewModel(
            new YgoCardInfoService(httpClient, directory),
            new BotProfileJsonService());
        viewModel.BeginNewRule();

        try
        {
            await viewModel.SaveProfileAsync(filePath);

            BotProfile loaded = await new BotProfileJsonService().ReadFileAsync(filePath);
            Assert.IsEmpty(loaded.Rules);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [TestMethod]
    public void RemoveMode_RejectsModeReferencedByRule()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var viewModel = new BotEditorViewModel(
            new YgoCardInfoService(httpClient, Path.GetTempPath()),
            new BotProfileJsonService());
        viewModel.BeginNewRule();
        viewModel.Draft.Name = "Transition";
        viewModel.Draft.Action = BotActionType.EndPhase;
        viewModel.Draft.NextModeId = "defensive";
        Assert.IsTrue(viewModel.ApplyRule());
        viewModel.SelectedMode = viewModel.Modes.Single(mode => mode.Id == "defensive");

        viewModel.RemoveSelectedMode();

        Assert.IsTrue(viewModel.Modes.Any(mode => mode.Id == "defensive"));
        StringAssert.Contains(viewModel.StatusMessage, "referenced by a rule");
    }

    [TestMethod]
    public async Task SearchCardsAsync_HttpFailureIsShownInStatusMessage()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var viewModel = new BotEditorViewModel(
            new YgoCardInfoService(httpClient, Path.GetTempPath()),
            new BotProfileJsonService());
        viewModel.SearchQuery = "Blue Eyes";

        await viewModel.SearchCardsAsync();

        StringAssert.Contains(viewModel.StatusMessage, "Card search failed:");
    }

    [TestMethod]
    public void ApplyRule_OrdersRulesByDescendingPriority()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var viewModel = new BotEditorViewModel(
            new YgoCardInfoService(httpClient, Path.GetTempPath()),
            new BotProfileJsonService());

        viewModel.BeginNewRule();
        viewModel.Draft.Name = "Low priority";
        viewModel.Draft.Action = BotActionType.EndPhase;
        viewModel.Draft.Priority = "5";
        Assert.IsTrue(viewModel.ApplyRule());

        viewModel.BeginNewRule();
        viewModel.Draft.Name = "High priority";
        viewModel.Draft.Action = BotActionType.EndPhase;
        viewModel.Draft.Priority = "50";
        Assert.IsTrue(viewModel.ApplyRule());

        Assert.AreEqual("High priority", viewModel.Rules[0].Name);
        Assert.AreEqual("Low priority", viewModel.Rules[1].Name);
    }

    [TestMethod]
    public void UseSelectedCardAsSubject_RegistersCardOutsideDeck()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var viewModel = new BotEditorViewModel(
            new YgoCardInfoService(httpClient, Path.GetTempPath()),
            new BotProfileJsonService());

        viewModel.SelectedCard = new CardModel { Id = 123, Name = "Experimental card" };
        viewModel.BeginNewRule();
        viewModel.UseSelectedCardAsSubject();

        Assert.AreEqual(BotRuleScope.Card, viewModel.Draft.Scope);
        Assert.AreEqual("123", viewModel.Draft.SubjectCardId);
        Assert.AreEqual("Experimental card", viewModel.Draft.SubjectCardName);
        Assert.AreEqual(123, viewModel.Cards.Single().CardId);
    }

    [TestMethod]
    public void RuleDraft_DoesNotMutateSavedRuleUntilApplied()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler());
        var viewModel = new BotEditorViewModel(
            new YgoCardInfoService(httpClient, Path.GetTempPath()),
            new BotProfileJsonService());
        viewModel.BeginNewRule();
        viewModel.Draft.Name = "Original rule";
        viewModel.Draft.Action = BotActionType.EndPhase;
        Assert.IsTrue(viewModel.ApplyRule());

        BotRule savedRule = viewModel.Rules[0];
        viewModel.Draft.Name = "Edited rule";

        Assert.AreEqual("Original rule", savedRule.Name);
        Assert.IsTrue(viewModel.ApplyRule());
        Assert.AreEqual("Edited rule", viewModel.Rules[0].Name);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler()
            : this(_ => new HttpResponseMessage(HttpStatusCode.NotFound))
        {
        }

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_responseFactory(request));
    }
}
