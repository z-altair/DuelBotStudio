using System.Text.Json.Serialization;

namespace DuelBotStudio.Models.Bot
{
    /// <summary>
    /// A prioritized, declarative bot decision rule.
    /// </summary>
    public class BotRule
    {
        /// <summary>Gets or sets the stable identifier for this rule.</summary>
        [JsonRequired]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>Gets or sets the user-facing rule name.</summary>
        [JsonRequired]
        public string Name { get; set; } = "New rule";

        /// <summary>Gets or sets whether this rule belongs to a card, group, or global policy.</summary>
        public BotRuleScope Scope { get; set; } = BotRuleScope.Global;

        /// <summary>Gets or sets the card that owns this rule when <see cref="Scope"/> is Card.</summary>
        public int? SubjectCardId { get; set; }

        /// <summary>Gets or sets the subject card name cached for display.</summary>
        public string SubjectCardName { get; set; } = string.Empty;

        /// <summary>Gets or sets the group that owns this rule when <see cref="Scope"/> is Group.</summary>
        public string? SubjectGroupId { get; set; }

        /// <summary>Gets or sets the mode in which this rule is eligible.</summary>
        [JsonRequired]
        public string ModeId { get; set; } = "combo";

        /// <summary>Gets or sets the decision callback at which this rule is considered.</summary>
        [JsonRequired]
        public BotDecisionPoint DecisionPoint { get; set; } = BotDecisionPoint.MainPhaseCommand;

        /// <summary>Gets or sets the conditions, all of which must match.</summary>
        [JsonRequired]
        public List<BotCondition> Conditions { get; set; } = new();

        /// <summary>Gets or sets the proposed action identifier.</summary>
        [JsonRequired]
        public BotActionType Action { get; set; }

        /// <summary>Gets or sets the optional card targeted by the action.</summary>
        public int? TargetCardId { get; set; }

        /// <summary>Gets or sets the target card name for display when available.</summary>
        public string TargetCardName { get; set; } = string.Empty;

        /// <summary>Gets or sets the rule priority; larger values take precedence.</summary>
        [JsonRequired]
        public int Priority { get; set; }

        /// <summary>Gets or sets the optional mode to activate after this rule matches.</summary>
        public string? NextModeId { get; set; }

        /// <summary>Gets or sets whether the rule is enabled.</summary>
        public bool IsEnabled { get; set; } = true;
    }
}
