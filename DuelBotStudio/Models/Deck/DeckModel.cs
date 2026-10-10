using DuelBotStudio.Models.Card;

namespace DuelBotStudio.Models.Deck
{
    public class DeckModel
    {
        public string Name { get; set; } = "Untitled Deck";
        public string FilePath { get; set; } = string.Empty;
        public List<CardModel> MainDeck { get; set; } = new List<CardModel>();
        public List<CardModel> ExtraDeck { get; set; } = new List<CardModel>();
        public List<CardModel> SideDeck { get; set; } = new List<CardModel>();

        // Useful info
        public int MainCount => MainDeck.Count;
        public int ExtraCount => ExtraDeck.Count;
        public int SideCount => SideDeck.Count;
        public int TotalCount => MainCount + ExtraCount + SideCount;
    }
}
