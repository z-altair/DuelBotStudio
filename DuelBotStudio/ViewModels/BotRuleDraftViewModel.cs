using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DuelBotStudio.Models.Bot;
using DuelBotStudio.Models.Card;

namespace DuelBotStudio.ViewModels
{
    /// <summary>Owns the temporary editable state for one bot rule.</summary>
    public sealed class BotRuleDraftViewModel : INotifyPropertyChanged
    {
        private bool _isResetting;
        private string _name = string.Empty;
        private BotDecisionPoint _decisionPoint;
        private BotActionType _action;
        private string _priority = "0";
        private string _modeId = string.Empty;
        private string? _nextModeId;
        private bool _isEnabled = true;
        private string _targetCardId = string.Empty;
        private string _targetCardName = string.Empty;
        private BotCondition? _selectedCondition;

        /// <summary>Creates an empty draft and starts observing its conditions.</summary>
        public BotRuleDraftViewModel()
        {
            Conditions.CollectionChanged += OnConditionsCollectionChanged;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised when editable rule data changes, excluding selection changes.</summary>
        public event EventHandler? DraftChanged;

        /// <summary>Gets or sets the rule name.</summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>Gets or sets the decision point.</summary>
        public BotDecisionPoint DecisionPoint
        {
            get => _decisionPoint;
            set => SetProperty(ref _decisionPoint, value);
        }

        /// <summary>Gets or sets the proposed action.</summary>
        public BotActionType Action
        {
            get => _action;
            set => SetProperty(ref _action, value);
        }

        /// <summary>Gets or sets the priority as editable text.</summary>
        public string Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        /// <summary>Gets or sets the mode in which the rule is active.</summary>
        public string ModeId
        {
            get => _modeId;
            set => SetProperty(ref _modeId, value);
        }

        /// <summary>Gets or sets the optional mode activated after a match.</summary>
        public string? NextModeId
        {
            get => _nextModeId;
            set => SetProperty(ref _nextModeId, value);
        }

        /// <summary>Gets or sets whether the rule is enabled.</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>Gets or sets the target card ID as editable text.</summary>
        public string TargetCardId
        {
            get => _targetCardId;
            set => SetProperty(ref _targetCardId, value);
        }

        /// <summary>Gets or sets the optional target card name for display.</summary>
        public string TargetCardName
        {
            get => _targetCardName;
            set => SetProperty(ref _targetCardName, value);
        }

        /// <summary>Gets the conditions that belong to this rule draft.</summary>
        public ObservableCollection<BotCondition> Conditions { get; } = new();

        /// <summary>Gets or sets the currently selected condition.</summary>
        public BotCondition? SelectedCondition
        {
            get => _selectedCondition;
            set => SetProperty(ref _selectedCondition, value, markDraftChanged: false);
        }

        /// <summary>Resets this draft to the default values for a new rule.</summary>
        public void Reset(string modeId)
        {
            _isResetting = true;
            Name = "New rule";
            DecisionPoint = BotDecisionPoint.MainPhaseCommand;
            Action = BotActionType.NormalSummon;
            Priority = "0";
            ModeId = modeId;
            NextModeId = null;
            IsEnabled = true;
            TargetCardId = string.Empty;
            TargetCardName = string.Empty;
            SelectedCondition = null;
            ClearConditions();
            _isResetting = false;
        }

        /// <summary>Loads an existing rule into this editable draft.</summary>
        public void Load(BotRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            _isResetting = true;
            Name = rule.Name;
            DecisionPoint = rule.DecisionPoint;
            Action = rule.Action;
            Priority = rule.Priority.ToString(CultureInfo.InvariantCulture);
            ModeId = rule.ModeId;
            NextModeId = rule.NextModeId;
            IsEnabled = rule.IsEnabled;
            TargetCardId = rule.TargetCardId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            TargetCardName = rule.TargetCardName;
            SelectedCondition = null;
            ClearConditions();
            foreach (BotCondition condition in rule.Conditions)
            {
                Conditions.Add(CloneCondition(condition));
            }

            _isResetting = false;
        }

        /// <summary>Adds an editable card-in-hand condition and selects it.</summary>
        public void AddCondition()
        {
            var condition = new BotCondition
            {
                Type = BotConditionType.CardInZone,
                Zone = BotCardZone.Hand
            };
            Conditions.Add(condition);
            SelectedCondition = condition;
        }

        /// <summary>Removes the selected condition if one is selected.</summary>
        public void RemoveSelectedCondition()
        {
            if (SelectedCondition is null)
            {
                return;
            }

            Conditions.Remove(SelectedCondition);
            SelectedCondition = null;
        }

        /// <summary>Uses a selected card as the rule's action target.</summary>
        public void SetTargetCard(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            TargetCardId = card.Id.ToString(CultureInfo.InvariantCulture);
            TargetCardName = card.Name;
        }

        /// <summary>Uses a selected card in the selected condition, creating one if needed.</summary>
        public void SetConditionCard(CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);
            SelectedCondition ??= CreateAndSelectCondition();
            SelectedCondition.CardId = card.Id;
            SelectedCondition.CardName = card.Name;
        }

        /// <summary>Builds a domain rule from the draft or returns an input error.</summary>
        public bool TryCreateRule(string ruleId, out BotRule? rule, out string? error)
        {
            rule = null;
            error = null;
            if (!int.TryParse(Priority, NumberStyles.Integer, CultureInfo.InvariantCulture, out int priority))
            {
                error = "Priority must be a whole number.";
                return false;
            }

            int? targetCardId = null;
            if (!string.IsNullOrWhiteSpace(TargetCardId))
            {
                if (!int.TryParse(TargetCardId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedId) ||
                    parsedId <= 0)
                {
                    error = "Target card ID must be a positive whole number.";
                    return false;
                }

                targetCardId = parsedId;
            }

            rule = new BotRule
            {
                Id = ruleId,
                Name = Name,
                ModeId = ModeId,
                DecisionPoint = DecisionPoint,
                Action = Action,
                Priority = priority,
                TargetCardId = targetCardId,
                TargetCardName = TargetCardName,
                NextModeId = string.IsNullOrWhiteSpace(NextModeId) ? null : NextModeId,
                IsEnabled = IsEnabled,
                Conditions = Conditions.Select(CloneCondition).ToList()
            };
            return true;
        }

        private BotCondition CreateAndSelectCondition()
        {
            AddCondition();
            return SelectedCondition!;
        }

        private void ClearConditions()
        {
            foreach (BotCondition condition in Conditions)
            {
                condition.PropertyChanged -= OnConditionChanged;
            }

            Conditions.Clear();
        }

        private void OnConditionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (BotCondition condition in e.OldItems)
                {
                    condition.PropertyChanged -= OnConditionChanged;
                }
            }

            if (e.NewItems is not null)
            {
                foreach (BotCondition condition in e.NewItems)
                {
                    condition.PropertyChanged += OnConditionChanged;
                }
            }

            RaiseChanged(nameof(Conditions), markDraftChanged: true);
        }

        private void OnConditionChanged(object? sender, PropertyChangedEventArgs e) =>
            RaiseChanged(nameof(Conditions), markDraftChanged: true);

        private void SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null,
            bool markDraftChanged = true)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            RaiseChanged(propertyName, markDraftChanged);
        }

        private void RaiseChanged(string? propertyName, bool markDraftChanged)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (!_isResetting && markDraftChanged)
            {
                DraftChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static BotCondition CloneCondition(BotCondition condition) => new()
        {
            Type = condition.Type,
            CardId = condition.CardId,
            CardName = condition.CardName,
            Zone = condition.Zone,
            MinimumCount = condition.MinimumCount
        };
    }
}
