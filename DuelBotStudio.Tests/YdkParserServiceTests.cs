using System.IO;
using DuelBotStudio.Services;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class YdkParserServiceTests
{
    private readonly YdkParserService _parser = new();

    [TestMethod]
    public void ParseFile_ParsesAllSectionsAndCreatorMetadata()
    {
        string path = WriteDeck("#created by Test User\n#main\n100\n#extra\n200\n!side\n300\n");
        try
        {
            var result = _parser.ParseFile(path);

            Assert.IsFalse(result.HasErrors);
            CollectionAssert.AreEqual(new[] { 100 }, result.MainDeckIds);
            CollectionAssert.AreEqual(new[] { 200 }, result.ExtraDeckIds);
            CollectionAssert.AreEqual(new[] { 300 }, result.SideDeckIds);
            CollectionAssert.AreEqual(new[] { 3 }, result.CardIdLineNumbers[100]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ParseFile_ReportsLineNumbersForInvalidContent()
    {
        string path = WriteDeck("#main\n12\nnot-a-card\n0\n!unknown\n");
        try
        {
            var result = _parser.ParseFile(path);

            Assert.IsTrue(result.HasErrors);
            Assert.HasCount(1, result.MainDeckIds);
            Assert.HasCount(3, result.Errors);
            StringAssert.Contains(result.Errors[0], "Line 3");
            StringAssert.Contains(result.Errors[1], "Line 4");
            StringAssert.Contains(result.Errors[2], "Line 5");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ParseFile_RejectsCardsBeforeASection()
    {
        string path = WriteDeck("42\n#main\n43\n");
        try
        {
            var result = _parser.ParseFile(path);

            Assert.IsTrue(result.HasErrors);
            CollectionAssert.AreEqual(new[] { 43 }, result.MainDeckIds);
            StringAssert.Contains(result.Errors[0], "Line 1");
            StringAssert.Contains(result.Errors[0], "before a deck section");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ParseFile_ReportsMissingFile()
    {
        var result = _parser.ParseFile(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydk"));

        Assert.IsTrue(result.HasErrors);
        StringAssert.Contains(result.Errors[0], "does not exist");
    }

    private static string WriteDeck(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydk");
        File.WriteAllText(path, content);
        return path;
    }
}
