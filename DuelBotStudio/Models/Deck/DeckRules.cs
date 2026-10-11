namespace DuelBotStudio.Models.Deck
{
    /// <summary>
    /// Configurable structural limits for deck validation. No banlist is applied.
    /// </summary>
    public sealed class DeckRules
    {
        /// <summary>Creates a set of deck limits.</summary>
        /// <param name="minimumMainDeckSize">Minimum allowed number of main-deck cards.</param>
        /// <param name="maximumMainDeckSize">Maximum allowed number of main-deck cards.</param>
        /// <param name="maximumExtraDeckSize">Maximum allowed number of extra-deck cards.</param>
        /// <param name="maximumSideDeckSize">Maximum allowed number of side-deck cards.</param>
        /// <param name="maximumCopiesPerCard">Maximum copies of one ID across all sections.</param>
        public DeckRules(
            int minimumMainDeckSize = 40,
            int maximumMainDeckSize = 60,
            int maximumExtraDeckSize = 15,
            int maximumSideDeckSize = 15,
            int maximumCopiesPerCard = 3)
        {
            if (minimumMainDeckSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumMainDeckSize));
            }

            if (maximumMainDeckSize < minimumMainDeckSize)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumMainDeckSize));
            }

            if (maximumExtraDeckSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumExtraDeckSize));
            }

            if (maximumSideDeckSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSideDeckSize));
            }

            if (maximumCopiesPerCard < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCopiesPerCard));
            }

            MinimumMainDeckSize = minimumMainDeckSize;
            MaximumMainDeckSize = maximumMainDeckSize;
            MaximumExtraDeckSize = maximumExtraDeckSize;
            MaximumSideDeckSize = maximumSideDeckSize;
            MaximumCopiesPerCard = maximumCopiesPerCard;
        }

        /// <summary>Gets the minimum main-deck size.</summary>
        public int MinimumMainDeckSize { get; }

        /// <summary>Gets the maximum main-deck size.</summary>
        public int MaximumMainDeckSize { get; }

        /// <summary>Gets the maximum extra-deck size.</summary>
        public int MaximumExtraDeckSize { get; }

        /// <summary>Gets the maximum side-deck size.</summary>
        public int MaximumSideDeckSize { get; }

        /// <summary>Gets the maximum copies of a card across all deck sections.</summary>
        public int MaximumCopiesPerCard { get; }
    }
}
