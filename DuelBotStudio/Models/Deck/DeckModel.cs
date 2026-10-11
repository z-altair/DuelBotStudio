using DuelBotStudio.Models.Card;

namespace DuelBotStudio.Models.Deck
{
    /// <summary>Represents the three sections of a constructed deck.</summary>
    public class DeckModel
    {
        /// <summary>Gets or sets the user-facing deck name.</summary>
        public string Name { get; set; } = "Untitled Deck";

        /// <summary>Gets or sets the source file path, when the deck was imported.</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Gets or sets the cards in the main deck.</summary>
        public List<CardModel> MainDeck { get; set; } = new List<CardModel>();

        /// <summary>Gets or sets the cards in the extra deck.</summary>
        public List<CardModel> ExtraDeck { get; set; } = new List<CardModel>();

        /// <summary>Gets or sets the cards in the side deck.</summary>
        public List<CardModel> SideDeck { get; set; } = new List<CardModel>();

        /// <summary>Gets the number of main-deck cards.</summary>
        public int MainCount => MainDeck.Count;

        /// <summary>Gets the number of extra-deck cards.</summary>
        public int ExtraCount => ExtraDeck.Count;

        /// <summary>Gets the number of side-deck cards.</summary>
        public int SideCount => SideDeck.Count;

        /// <summary>Gets the total number of cards in all sections.</summary>
        public int TotalCount => MainCount + ExtraCount + SideCount;
    }
}
