using DuelBotStudio.Models.Deck;

namespace DuelBotStudio.Models.Bot
{
    /// <summary>
    /// Reserved bot profile data. Bot behavior, execution, and persistence are not implemented.
    /// </summary>
    public class BotProfile
    {
        /// <summary>Gets or sets the profile name.</summary>
        public string BotName { get; set; } = "Custom Bot";

        /// <summary>Gets or sets the deck associated with the profile.</summary>
        public DeckModel AssociatedDeck { get; set; } = new DeckModel();

        /// <summary>Gets or sets the unexecuted draft rules associated with the profile.</summary>
        public List<BotRule> Rules { get; set; } = new List<BotRule>();
    }
}
