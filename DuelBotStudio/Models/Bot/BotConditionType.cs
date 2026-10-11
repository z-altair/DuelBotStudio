namespace DuelBotStudio.Models.Bot
{
    /// <summary>Typed predicates supported by the first bot profile schema.</summary>
    public enum BotConditionType
    {
        /// <summary>A card with the selected ID is present in the chosen zone.</summary>
        CardInZone,

        /// <summary>A chain response is available to the bot.</summary>
        OpponentHasChainToRespond,

        /// <summary>The selected card's effect has been negated.</summary>
        EffectWasNegated,

        /// <summary>The selected card's effect has been used at least a configured number of times.</summary>
        EffectUseCountAtLeast
    }
}
