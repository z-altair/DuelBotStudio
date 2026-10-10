using DuelBotStudio.Models.Card;

namespace DuelBotStudio.Models.Deck
{
    public class DeckModel
    {
        public string name { get; set; } = "Untitled Deck";
        public string filePath { get; set; } = string.Empty;
        public List<CardModel> mainDeck { get; set; } = new List<CardModel>();
        public List<CardModel> extraDeck { get; set; } = new List<CardModel>();
        public List<CardModel> sideDeck { get; set; } = new List<CardModel>();

        // Useful info
        public int mainCount => mainDeck.Count;
        public int extraCount => extraDeck.Count;
        public int sideCount => sideDeck.Count;
        public int totalCount => mainCount + extraCount + sideCount;
    }
}
