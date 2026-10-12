namespace DuelBotStudio.Models.Bot
{
    /// <summary>Identifies the owner of a bot rule.</summary>
    public enum BotRuleScope
    {
        /// <summary>The rule describes the behavior of one specific card.</summary>
        Card,

        /// <summary>The rule describes behavior shared by a user-defined card group.</summary>
        Group,

        /// <summary>The rule describes behavior that is not tied to a card.</summary>
        Global
    }
}
