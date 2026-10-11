namespace DuelBotStudio.Models.Bot
{
    /// <summary>
    /// Declarative actions that a compatible bot runtime may execute.
    /// </summary>
    public enum BotActionType
    {
        /// <summary>Normal summon action.</summary>
        NormalSummon,

        /// <summary>Special summon action.</summary>
        SpecialSummon,

        /// <summary>Card activation action.</summary>
        ActivateEffect,

        /// <summary>Set a monster action.</summary>
        SetMonster,

        /// <summary>Set a spell or trap action.</summary>
        SetSpellTrap,

        /// <summary>Change a monster's battle position action.</summary>
        Reposition,

        /// <summary>Declare an attack.</summary>
        Attack,

        /// <summary>Decline an optional chain.</summary>
        PassChain,

        /// <summary>Move to the battle phase.</summary>
        EnterBattlePhase,

        /// <summary>End the current turn.</summary>
        EndPhase
    }
}
