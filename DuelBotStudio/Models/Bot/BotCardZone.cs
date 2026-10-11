namespace DuelBotStudio.Models.Bot
{
    /// <summary>A card location that may be inspected by a bot rule.</summary>
    public enum BotCardZone
    {
        /// <summary>The player's hand.</summary>
        Hand,

        /// <summary>A card on the duel field.</summary>
        Field,

        /// <summary>The player's graveyard.</summary>
        Graveyard,

        /// <summary>The banished-card area.</summary>
        Banished
    }
}
