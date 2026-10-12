using System.IO;
using DuelBotStudio.Models.Bot;
using DuelBotStudio.Services;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class BotGroupLibraryJsonServiceTests
{
    [TestMethod]
    public void SerializeRoundTrip_PreservesGroupsAndRepresentativeCard()
    {
        var service = new BotGroupLibraryJsonService();
        var library = new BotGroupLibrary();
        library.Groups.Add(new BotCardGroup
        {
            Id = "negations",
            Name = "Negations",
            Description = "Cards that interrupt the opponent.",
            CardIds = new List<int> { 1, 2 },
            RepresentativeCardId = 1
        });

        BotGroupLibrary result = service.Deserialize(service.Serialize(library));

        Assert.AreEqual("Negations", result.Groups[0].Name);
        Assert.AreEqual(1, result.Groups[0].RepresentativeCardId);
        Assert.HasCount(2, result.Groups[0].CardIds);
    }

    [TestMethod]
    public void Serialize_RejectsDuplicateGroupIds()
    {
        var service = new BotGroupLibraryJsonService();
        var library = new BotGroupLibrary
        {
            Groups =
            [
                new BotCardGroup { Id = "same", Name = "One" },
                new BotCardGroup { Id = "same", Name = "Two" }
            ]
        };

        Assert.Throws<InvalidDataException>(() => service.Serialize(library));
    }
}
