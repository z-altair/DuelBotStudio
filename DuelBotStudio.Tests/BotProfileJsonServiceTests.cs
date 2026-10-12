using System.IO;
using System.Text.Json;
using DuelBotStudio.Models.Bot;
using DuelBotStudio.Services;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class BotProfileJsonServiceTests
{
    private readonly BotProfileJsonService _service = new();

    [TestMethod]
    public void Serialize_UsesVersionedStringEnumsAndDoesNotEmbedDecks()
    {
        BotProfile profile = CreateValidProfile();

        string json = _service.Serialize(profile);

        StringAssert.Contains(json, "\"schemaVersion\": 1");
        StringAssert.Contains(json, "\"action\": \"normalSummon\"");
        StringAssert.Contains(json, "\"type\": \"cardInZone\"");
        StringAssert.Contains(json, "\"zone\": \"hand\"");
        StringAssert.Contains(json, "\"nextModeId\": \"defensive\"");
        Assert.IsFalse(json.Contains("associatedDeck", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Serialize_RoundTripsCardCatalogGroupsAndScopedRules()
    {
        BotProfile profile = CreateValidProfile();
        profile.Cards.Add(new BotCardProfile
        {
            CardId = 123,
            CardName = "Negation card",
            Notes = "Keep this available for important activations."
        });
        profile.Groups.Add(new BotCardGroup
        {
            Id = "negations",
            Name = "Negations",
            CardIds = [123],
            Priority = 100
        });
        profile.Rules[0].Scope = BotRuleScope.Card;
        profile.Rules[0].SubjectCardId = 123;
        profile.Rules[0].SubjectCardName = "Negation card";

        BotProfile result = _service.Deserialize(_service.Serialize(profile));

        Assert.HasCount(1, result.Cards);
        Assert.AreEqual("Negations", result.Groups[0].Name);
        Assert.AreEqual(BotRuleScope.Card, result.Rules[0].Scope);
        Assert.AreEqual(123, result.Rules[0].SubjectCardId);
    }

    [TestMethod]
    public void Serialize_RejectsGroupRuleWithoutDefinedGroup()
    {
        BotProfile profile = CreateValidProfile();
        profile.Rules[0].Scope = BotRuleScope.Group;
        profile.Rules[0].SubjectGroupId = "missing-group";

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => _service.Serialize(profile));

        StringAssert.Contains(exception.Message, "group ID");
    }

    [TestMethod]
    public void Deserialize_RoundTripsConditionsAndModeTransitions()
    {
        BotProfile original = CreateValidProfile();

        BotProfile result = _service.Deserialize(_service.Serialize(original));

        Assert.AreEqual(BotProfile.CurrentSchemaVersion, result.SchemaVersion);
        Assert.AreEqual("defensive", result.Rules[0].NextModeId);
        Assert.AreEqual(BotConditionType.CardInZone, result.Rules[0].Conditions[0].Type);
        Assert.AreEqual(89631139, result.Rules[0].Conditions[0].CardId);
        Assert.AreEqual(BotCardZone.Hand, result.Rules[0].Conditions[0].Zone);
    }

    [TestMethod]
    public void Deserialize_RequiresSchemaVersion()
    {
        const string json = """{"botName":"Old profile","initialModeId":"combo","modes":[{"id":"combo","name":"Combo"}],"rules":[]}""";

        Assert.Throws<JsonException>(() => _service.Deserialize(json));
    }

    [TestMethod]
    public void Deserialize_RequiresProfileCollectionsAndRuleProperties()
    {
        string missingRules = _service.Serialize(CreateValidProfile())
            .Replace("  \"rules\": [", "  \"unexpected\": [", StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => _service.Deserialize(missingRules));

        string missingPriority = _service.Serialize(CreateValidProfile())
            .Replace("      \"priority\": 20,\r\n", string.Empty, StringComparison.Ordinal)
            .Replace("      \"priority\": 20,\n", string.Empty, StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => _service.Deserialize(missingPriority));
    }

    [TestMethod]
    public void Deserialize_RejectsUnknownSchemaVersionAndEnumValues()
    {
        string unsupportedVersion = _service.Serialize(CreateValidProfile())
            .Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => _service.Deserialize(unsupportedVersion));

        string unknownEnum = _service.Serialize(CreateValidProfile())
            .Replace("\"normalSummon\"", "\"futureSummon\"", StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => _service.Deserialize(unknownEnum));
    }

    [TestMethod]
    public void Deserialize_RejectsUnknownPropertiesInsteadOfSilentlyDroppingThem()
    {
        string json = _service.Serialize(CreateValidProfile())
            .Replace("\"botName\": \"Combo bot\",", "\"botName\": \"Combo bot\",\n  \"futureRuntimeData\": true,", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => _service.Deserialize(json));
    }

    [TestMethod]
    public void Serialize_RejectsBrokenModeReferences()
    {
        BotProfile profile = CreateValidProfile();
        profile.Rules[0].NextModeId = "missing-mode";

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => _service.Serialize(profile));

        StringAssert.Contains(exception.Message, "next mode must reference a defined bot mode");
    }

    [TestMethod]
    public async Task WriteFileAsync_InvalidProfileDoesNotReplaceExistingFile()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(filePath, "existing profile");
        BotProfile profile = CreateValidProfile();
        profile.Rules[0].TargetCardId = null;

        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => _service.WriteFileAsync(profile, filePath));
            Assert.AreEqual("existing profile", await File.ReadAllTextAsync(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task ReadAndWriteFileAsync_PersistAValidProfile()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        BotProfile profile = CreateValidProfile();

        try
        {
            await _service.WriteFileAsync(profile, filePath);
            BotProfile loaded = await _service.ReadFileAsync(filePath);

            Assert.AreEqual(profile.BotName, loaded.BotName);
            Assert.AreEqual(profile.Rules[0].TargetCardId, loaded.Rules[0].TargetCardId);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static BotProfile CreateValidProfile()
    {
        var profile = new BotProfile { BotName = "Combo bot" };
        profile.Rules.Add(new BotRule
        {
            Id = "summon-blue-eyes",
            Name = "Summon Blue-Eyes",
            ModeId = "combo",
            DecisionPoint = BotDecisionPoint.MainPhaseCommand,
            Action = BotActionType.NormalSummon,
            TargetCardId = 89631139,
            TargetCardName = "Blue-Eyes White Dragon",
            Priority = 20,
            NextModeId = "defensive",
            Conditions =
            [
                new BotCondition
                {
                    Type = BotConditionType.CardInZone,
                    CardId = 89631139,
                    CardName = "Blue-Eyes White Dragon",
                    Zone = BotCardZone.Hand
                }
            ]
        });
        return profile;
    }
}
