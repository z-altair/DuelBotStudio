using DuelBotStudio.Models.Bot;

namespace DuelBotStudio.Services
{
    /// <summary>Validates bot profiles before they are persisted or consumed.</summary>
    public sealed class BotProfileValidator
    {
        /// <summary>Returns validation errors without modifying the profile.</summary>
        public IReadOnlyList<string> Validate(BotProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            var errors = new List<string>();

            if (profile.SchemaVersion != BotProfile.CurrentSchemaVersion)
            {
                errors.Add($"Unsupported bot profile schema version: {profile.SchemaVersion}.");
            }

            if (string.IsNullOrWhiteSpace(profile.BotName))
            {
                errors.Add("The bot name is required.");
            }

            if (profile.Modes is null || profile.Modes.Count == 0)
            {
                errors.Add("At least one bot mode is required.");
                return errors;
            }

            var modeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (BotMode? mode in profile.Modes)
            {
                if (mode is null || string.IsNullOrWhiteSpace(mode.Id))
                {
                    errors.Add("Every bot mode must have an ID.");
                    continue;
                }

                if (!modeIds.Add(mode.Id))
                {
                    errors.Add($"Duplicate bot mode ID: {mode.Id}.");
                }

                if (string.IsNullOrWhiteSpace(mode.Name))
                {
                    errors.Add($"Bot mode '{mode.Id}' must have a name.");
                }
            }

            if (string.IsNullOrWhiteSpace(profile.InitialModeId) || !modeIds.Contains(profile.InitialModeId))
            {
                errors.Add("The initial mode must reference a defined bot mode.");
            }

            if (profile.Rules is null)
            {
                errors.Add("The rules collection cannot be null.");
                return errors;
            }

            var ruleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (BotRule? rule in profile.Rules)
            {
                if (rule is null)
                {
                    errors.Add("The rules collection cannot contain null entries.");
                    continue;
                }

                ValidateRule(rule, modeIds, ruleIds, errors);
            }

            return errors;
        }

        private static void ValidateRule(
            BotRule rule,
            HashSet<string> modeIds,
            HashSet<string> ruleIds,
            List<string> errors)
        {
            string label = string.IsNullOrWhiteSpace(rule.Name) ? "Unnamed rule" : rule.Name;
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                errors.Add($"{label}: a stable rule ID is required.");
            }
            else if (!ruleIds.Add(rule.Id))
            {
                errors.Add($"{label}: duplicate rule ID '{rule.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                errors.Add("Every rule must have a name.");
            }

            if (string.IsNullOrWhiteSpace(rule.ModeId) || !modeIds.Contains(rule.ModeId))
            {
                errors.Add($"{label}: its active mode must reference a defined bot mode.");
            }

            if (!Enum.IsDefined(rule.DecisionPoint))
            {
                errors.Add($"{label}: its decision point is not supported.");
            }

            if (!Enum.IsDefined(rule.Action))
            {
                errors.Add($"{label}: its action is not supported.");
            }

            if (RequiresTargetCard(rule.Action) && (!rule.TargetCardId.HasValue || rule.TargetCardId <= 0))
            {
                errors.Add($"{label}: the selected action requires a target card with a valid ID.");
            }

            if (rule.Action == BotActionType.PassChain && rule.DecisionPoint != BotDecisionPoint.ChainSelection)
            {
                errors.Add($"{label}: PassChain is only valid at chain selection.");
            }

            if (rule.Action == BotActionType.Attack && rule.DecisionPoint != BotDecisionPoint.BattleCommand)
            {
                errors.Add($"{label}: Attack is only valid at battle command selection.");
            }

            if (rule.Action is BotActionType.NormalSummon
                or BotActionType.SpecialSummon
                or BotActionType.SetMonster
                or BotActionType.SetSpellTrap
                or BotActionType.Reposition
                or BotActionType.EnterBattlePhase &&
                rule.DecisionPoint != BotDecisionPoint.MainPhaseCommand)
            {
                errors.Add($"{label}: this action is only valid at main-phase command selection.");
            }

            if (rule.Action == BotActionType.ActivateEffect &&
                rule.DecisionPoint is not (BotDecisionPoint.MainPhaseCommand or BotDecisionPoint.ChainSelection))
            {
                errors.Add($"{label}: ActivateEffect is only valid at main-phase or chain selection.");
            }

            if (rule.Action == BotActionType.EndPhase &&
                rule.DecisionPoint is not (BotDecisionPoint.MainPhaseCommand or BotDecisionPoint.BattleCommand))
            {
                errors.Add($"{label}: EndPhase is only valid at main-phase or battle command selection.");
            }

            if (rule.NextModeId is not null &&
                (string.IsNullOrWhiteSpace(rule.NextModeId) || !modeIds.Contains(rule.NextModeId)))
            {
                errors.Add($"{label}: its next mode must reference a defined bot mode.");
            }

            if (rule.Conditions is null)
            {
                errors.Add($"{label}: the conditions collection cannot be null.");
                return;
            }

            foreach (BotCondition? condition in rule.Conditions)
            {
                if (condition is null)
                {
                    errors.Add($"{label}: the conditions collection cannot contain null entries.");
                    continue;
                }

                ValidateCondition(rule, condition, label, errors);
            }
        }

        private static void ValidateCondition(BotRule rule, BotCondition condition, string label, List<string> errors)
        {
            if (!Enum.IsDefined(condition.Type))
            {
                errors.Add($"{label}: a condition type is not supported.");
                return;
            }

            switch (condition.Type)
            {
                case BotConditionType.CardInZone:
                    if (!HasValidCardId(condition))
                    {
                        errors.Add($"{label}: CardInZone requires a valid card ID.");
                    }

                    if (!condition.Zone.HasValue || !Enum.IsDefined(condition.Zone.Value))
                    {
                        errors.Add($"{label}: CardInZone requires a valid card zone.");
                    }

                    break;
                case BotConditionType.OpponentHasChainToRespond:
                    if (rule.DecisionPoint != BotDecisionPoint.ChainSelection)
                    {
                        errors.Add($"{label}: OpponentHasChainToRespond requires chain selection.");
                    }

                    break;
                case BotConditionType.EffectWasNegated:
                    if (!HasValidCardId(condition))
                    {
                        errors.Add($"{label}: EffectWasNegated requires a valid card ID.");
                    }

                    break;
                case BotConditionType.EffectUseCountAtLeast:
                    if (!HasValidCardId(condition))
                    {
                        errors.Add($"{label}: EffectUseCountAtLeast requires a valid card ID.");
                    }

                    if (!condition.MinimumCount.HasValue || condition.MinimumCount < 1)
                    {
                        errors.Add($"{label}: EffectUseCountAtLeast requires a minimum count of at least 1.");
                    }

                    break;
            }
        }

        private static bool HasValidCardId(BotCondition condition) =>
            condition.CardId.HasValue && condition.CardId.Value > 0;

        private static bool RequiresTargetCard(BotActionType action) =>
            action is BotActionType.NormalSummon
                or BotActionType.SpecialSummon
                or BotActionType.ActivateEffect
                or BotActionType.SetMonster
                or BotActionType.SetSpellTrap
                or BotActionType.Reposition
                or BotActionType.Attack;
    }
}
