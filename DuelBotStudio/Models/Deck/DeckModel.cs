using DuelBotStudio.Models.Card;
using System.Collections.ObjectModel;

namespace DuelBotStudio.Models.Deck
{
    /// <summary>Represents the three sections of a constructed deck.</summary>
    public class DeckModel
    {
        private readonly List<CardModel> _mainDeck = new();
        private readonly List<CardModel> _extraDeck = new();
        private readonly List<CardModel> _sideDeck = new();

        /// <summary>Creates an empty deck with read-only section views.</summary>
        public DeckModel()
        {
            MainDeck = _mainDeck.AsReadOnly();
            ExtraDeck = _extraDeck.AsReadOnly();
            SideDeck = _sideDeck.AsReadOnly();
        }

        /// <summary>Gets or sets the user-facing deck name.</summary>
        public string Name { get; set; } = "Untitled Deck";

        /// <summary>Gets the source file path, when the deck was imported or saved.</summary>
        public string FilePath { get; private set; } = string.Empty;

        /// <summary>Gets a read-only view of the main-deck cards.</summary>
        public ReadOnlyCollection<CardModel> MainDeck { get; }

        /// <summary>Gets a read-only view of the extra-deck cards.</summary>
        public ReadOnlyCollection<CardModel> ExtraDeck { get; }

        /// <summary>Gets a read-only view of the side-deck cards.</summary>
        public ReadOnlyCollection<CardModel> SideDeck { get; }

        /// <summary>Gets the number of main-deck cards.</summary>
        public int MainCount => MainDeck.Count;

        /// <summary>Gets the number of extra-deck cards.</summary>
        public int ExtraCount => ExtraDeck.Count;

        /// <summary>Gets the number of side-deck cards.</summary>
        public int SideCount => SideDeck.Count;

        /// <summary>Gets the total number of cards in all sections.</summary>
        public int TotalCount => MainCount + ExtraCount + SideCount;

        internal void AddCard(CardModel card, DeckSection section) => GetMutableSection(section).Add(card);

        internal bool RemoveCard(DeckSection section, CardModel card) => GetMutableSection(section).Remove(card);

        internal void SetFilePath(string filePath) => FilePath = filePath;

        private List<CardModel> GetMutableSection(DeckSection section) => section switch
        {
            DeckSection.Main => _mainDeck,
            DeckSection.Extra => _extraDeck,
            DeckSection.Side => _sideDeck,
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown deck section.")
        };
    }
}
