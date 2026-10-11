namespace DuelBotStudio.Models.Bot
{
    /// <summary>
    /// Placeholder action identifiers for a future bot design; no action is executed today.
    /// </summary>
    public enum BotActionType
    {
        /// <summary>Normal summon action.</summary>
        Summon,

        /// <summary>Special summon action.</summary>
        SpSummon,

        /// <summary>Card activation action.</summary>
        Activate,

        /// <summary>Set a spell or trap action.</summary>
        SetSpellTrap,

        /// <summary>Change a monster's battle position action.</summary>
        Repos
    }
}
