namespace DuelBotStudio.Models.Bot
{
    /// <summary>Identifies a family of decisions made by a bot runtime.</summary>
    public enum BotDecisionPoint
    {
        /// <summary>Main-phase action selection.</summary>
        MainPhaseCommand,

        /// <summary>Battle-phase action selection.</summary>
        BattleCommand,

        /// <summary>Optional or forced chain selection.</summary>
        ChainSelection
    }
}
