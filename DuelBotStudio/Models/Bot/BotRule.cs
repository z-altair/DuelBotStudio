namespace DuelBotStudio.Models.Bot
{
    /// <summary>
    /// Reserved rule data for future bot design; condition expressions are not evaluated.
    /// </summary>
    public class BotRule
    {
        /// <summary>Gets or sets the card identifier targeted by the draft rule.</summary>
        public int CardId { get; set; }

        /// <summary>Gets or sets the card name displayed with the rule.</summary>
        public string CardName { get; set; } = string.Empty;

        /// <summary>Gets or sets the proposed action identifier.</summary>
        public BotActionType Action { get; set; }

        /// <summary>Gets or sets the unparsed condition text.</summary>
        public string ConditionExpression { get; set; } = string.Empty;

        /// <summary>Gets or sets the proposed execution priority.</summary>
        public int Priority { get; set; }

    }
}
