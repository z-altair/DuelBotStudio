using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using DuelBotStudio.Models.Bot;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Services;

namespace DuelBotStudio.ViewModels
{
    /// <summary>Coordinates bot profile editing, card lookup, validation, and JSON persistence.</summary>
    public sealed class BotEditorViewModel : ObservableObject
    {
        private readonly YgoCardInfoService _cardInfoService;
        private readonly BotProfileJsonService _profileService;
        private int _searchGeneration;
        private bool _isLoading;
        private bool _isDirty;
        private string _botName;
        private string _initialModeId;
        private string _searchQuery = string.Empty;
        private string _statusMessage = string.Empty;
        private string? _filePath;
        private bool _isRuleDraftActive;
        private BotMode? _selectedMode;
        private BotCardGroup? _selectedGroup;
        private BotRule? _selectedRule;
        private CardModel? _selectedCard;

        /// <summary>Creates the bot editor view model.</summary>
        public BotEditorViewModel(YgoCardInfoService cardInfoService, BotProfileJsonService profileService)
        {
            _cardInfoService = cardInfoService ?? throw new ArgumentNullException(nameof(cardInfoService));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            var defaultProfile = new BotProfile();
            _botName = defaultProfile.BotName;
            _initialModeId = defaultProfile.InitialModeId;
            Draft = new BotRuleDraftViewModel();
            Draft.DraftChanged += OnDraftChanged;

            foreach (BotMode mode in defaultProfile.Modes)
            {
                AddModeToCollection(mode);
            }

            Modes.CollectionChanged += (_, _) => MarkDirty();
            Cards.CollectionChanged += (_, _) => MarkDirty();
            Groups.CollectionChanged += (_, _) => MarkDirty();
            Rules.CollectionChanged += (_, _) => MarkDirty();
        }

        /// <summary>Gets the available decision points.</summary>
        public IReadOnlyList<BotDecisionPoint> DecisionPoints { get; } = Enum.GetValues<BotDecisionPoint>();

        /// <summary>Gets the available rule actions.</summary>
        public IReadOnlyList<BotActionType> Actions { get; } = Enum.GetValues<BotActionType>();

        /// <summary>Gets the available condition types.</summary>
        public IReadOnlyList<BotConditionType> ConditionTypes { get; } = Enum.GetValues<BotConditionType>();

        /// <summary>Gets the available card zones.</summary>
        public IReadOnlyList<BotCardZone> CardZones { get; } = Enum.GetValues<BotCardZone>();

        /// <summary>Gets the available rule owner scopes.</summary>
        public IReadOnlyList<BotRuleScope> RuleScopes { get; } = Enum.GetValues<BotRuleScope>();

        /// <summary>Gets the persistent card behavior profiles.</summary>
        public ObservableCollection<BotCardProfile> Cards { get; } = new();

        /// <summary>Gets the reusable card groups.</summary>
        public ObservableCollection<BotCardGroup> Groups { get; } = new();

        /// <summary>Gets the editor for the currently selected rule draft.</summary>
        public BotRuleDraftViewModel Draft { get; }

        /// <summary>Gets the editable profile modes.</summary>
        public ObservableCollection<BotMode> Modes { get; } = new();

        /// <summary>Gets the profile rules.</summary>
        public ObservableCollection<BotRule> Rules { get; } = new();

        /// <summary>Gets the current card search results.</summary>
        public ObservableCollection<CardModel> SearchResults { get; } = new();

        /// <summary>Gets or sets the profile name.</summary>
        public string BotName
        {
            get => _botName;
            set
            {
                if (SetProperty(ref _botName, value))
                {
                    MarkDirty();
                }
            }
        }

        /// <summary>Gets or sets the ID of the starting mode.</summary>
        public string InitialModeId
        {
            get => _initialModeId;
            set
            {
                if (SetProperty(ref _initialModeId, value))
                {
                    MarkDirty();
                }
            }
        }

        /// <summary>Gets or sets the selected mode.</summary>
        public BotMode? SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value);
        }

        /// <summary>Gets or sets the selected editable card group.</summary>
        public BotCardGroup? SelectedGroup
        {
            get => _selectedGroup;
            set => SetProperty(ref _selectedGroup, value);
        }

        /// <summary>Gets or sets the selected rule.</summary>
        public BotRule? SelectedRule
        {
            get => _selectedRule;
            set
            {
                if (SetProperty(ref _selectedRule, value))
                {
                    if (value is null)
                    {
                        return;
                    }

                    LoadRuleDraft(value);
                }
            }
        }

        /// <summary>Gets or sets the current card search query.</summary>
        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        /// <summary>Gets or sets the selected card search result.</summary>
        public CardModel? SelectedCard
        {
            get => _selectedCard;
            set => SetProperty(ref _selectedCard, value);
        }

        /// <summary>Gets the path of the loaded or last-saved profile.</summary>
        public string? FilePath
        {
            get => _filePath;
            private set => SetProperty(ref _filePath, value);
        }

        /// <summary>Gets the user-facing status or error message.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>Gets validation errors for the current profile.</summary>
        public IReadOnlyList<string> ValidationErrors { get; private set; } = Array.Empty<string>();

        /// <summary>Gets whether profile changes have not yet been saved.</summary>
        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
        }

        /// <summary>Searches for cards to reference by their stable card IDs.</summary>
        public async Task SearchCardsAsync()
        {
            int generation = ++_searchGeneration;
            if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Trim().Length < 3)
            {
                SearchResults.Clear();
                SelectedCard = null;
                StatusMessage = "Enter at least three characters to search.";
                return;
            }

            StatusMessage = "Searching...";
            SearchResults.Clear();
            SelectedCard = null;
            try
            {
                List<CardModel> cards = await _cardInfoService.SearchCardsByNameAsync(SearchQuery);
                if (generation != _searchGeneration)
                {
                    return;
                }

                foreach (CardModel card in cards)
                {
                    SearchResults.Add(card);
                }

                StatusMessage = cards.Count == 0
                    ? "No cards matched the search."
                    : $"{cards.Count} card(s) found. Select one to use its ID.";
            }
            catch (HttpRequestException exception)
            {
                if (generation == _searchGeneration)
                {
                    StatusMessage = $"Card search failed: {exception.Message}";
                }
            }
            catch (IOException exception)
            {
                if (generation == _searchGeneration)
                {
                    StatusMessage = $"Card cache could not be accessed: {exception.Message}";
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                if (generation == _searchGeneration)
                {
                    StatusMessage = $"Card cache access was denied: {exception.Message}";
                }
            }
            catch (JsonException exception)
            {
                if (generation == _searchGeneration)
                {
                    StatusMessage = $"Card data could not be read: {exception.Message}";
                }
            }
        }

        /// <summary>Begins a new rule draft.</summary>
        public void BeginNewRule()
        {
            _isRuleDraftActive = false;
            SelectedRule = null;
            Draft.Reset(InitialModeId);
            StatusMessage = "Enter the rule details, then apply the rule.";
        }

        /// <summary>Creates a new mode for a distinct strategy such as a defensive plan.</summary>
        public void AddMode()
        {
            var mode = new BotMode
            {
                Id = $"mode-{Guid.NewGuid():N}",
                Name = "New mode"
            };
            AddModeToCollection(mode);
            SelectedMode = mode;
        }

        /// <summary>Creates an empty reusable card group.</summary>
        public void AddGroup()
        {
            var group = new BotCardGroup
            {
                Id = $"group-{Guid.NewGuid():N}",
                Name = "New group"
            };
            Groups.Add(group);
            SelectedGroup = group;
            StatusMessage = "Card group created.";
        }

        /// <summary>Removes the selected card group when no rule references it.</summary>
        public void RemoveSelectedGroup()
        {
            if (SelectedGroup is null)
            {
                return;
            }

            if (Rules.Any(rule => rule.Scope == BotRuleScope.Group &&
                                  rule.SubjectGroupId == SelectedGroup.Id))
            {
                StatusMessage = "The selected group is referenced by a rule.";
                return;
            }

            Groups.Remove(SelectedGroup);
            foreach (BotCardProfile card in Cards)
            {
                card.GroupIds?.Remove(SelectedGroup.Id);
            }
            SelectedGroup = Groups.FirstOrDefault();
            StatusMessage = "Card group removed.";
        }


        /// <summary>Removes the selected mode when no profile reference depends on it.</summary>
        public void RemoveSelectedMode()
        {
            if (SelectedMode is null)
            {
                return;
            }

            BotMode selected = SelectedMode;
            if (Modes.Count <= 1 ||
                InitialModeId == selected.Id ||
                Rules.Any(rule => rule.ModeId == selected.Id || rule.NextModeId == selected.Id) ||
                (_isRuleDraftActive && (Draft.ModeId == selected.Id || Draft.NextModeId == selected.Id)))
            {
                StatusMessage = "This mode is the initial mode or is referenced by a rule; update those references first.";
                return;
            }

            Modes.Remove(selected);
            selected.PropertyChanged -= OnModeChanged;
            SelectedMode = Modes.FirstOrDefault();
            StatusMessage = "Mode removed.";
        }

        /// <summary>Applies the selected card to the action target of the current rule draft.</summary>
        public void UseSelectedCardAsTarget()
        {
            if (SelectedCard is null)
            {
                StatusMessage = "Select a card from the search results first.";
                return;
            }

            Draft.SetTargetCard(SelectedCard);
            StatusMessage = $"Action target set to {SelectedCard.Name} ({SelectedCard.Id}).";
        }

        /// <summary>Registers the selected card without requiring it to be in the current deck.</summary>
        public void RegisterSelectedCard()
        {
            if (SelectedCard is null)
            {
                StatusMessage = "Select a card from the search results first.";
                return;
            }

            if (Cards.All(card => card.CardId != SelectedCard.Id))
            {
                Cards.Add(new BotCardProfile
                {
                    CardId = SelectedCard.Id,
                    CardName = SelectedCard.Name
                });
                StatusMessage = $"{SelectedCard.Name} was added to the bot catalog.";
            }
            else
            {
                StatusMessage = $"{SelectedCard.Name} is already in the bot catalog.";
            }
        }

        /// <summary>Adds the selected card to the selected reusable group.</summary>
        public void AddSelectedCardToGroup()
        {
            if (SelectedCard is null || SelectedGroup is null)
            {
                StatusMessage = "Select both a card and a card group first.";
                return;
            }

            if (!SelectedGroup.CardIds.Contains(SelectedCard.Id))
            {
                SelectedGroup.CardIds.Add(SelectedCard.Id);
                RegisterSelectedCard();
                BotCardProfile cardProfile = Cards.Single(card => card.CardId == SelectedCard.Id);
                cardProfile.GroupIds ??= new List<string>();
                if (!cardProfile.GroupIds.Contains(SelectedGroup.Id))
                {
                    cardProfile.GroupIds.Add(SelectedGroup.Id);
                }
                StatusMessage = $"{SelectedCard.Name} added to {SelectedGroup.Name}.";
                MarkDirty();
            }
            else
            {
                StatusMessage = $"{SelectedCard.Name} is already in {SelectedGroup.Name}.";
            }
        }

        /// <summary>Uses the selected card as the owner of the current rule.</summary>
        public void UseSelectedCardAsSubject()
        {
            if (SelectedCard is null)
            {
                StatusMessage = "Select a card from the search results first.";
                return;
            }

            RegisterSelectedCard();
            Draft.Scope = BotRuleScope.Card;
            Draft.SubjectCardId = SelectedCard.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Draft.SubjectCardName = SelectedCard.Name;
            StatusMessage = $"Rule subject set to {SelectedCard.Name}.";
        }

        /// <summary>Applies the selected card to the selected card-specific condition.</summary>
        public void UseSelectedCardInCondition()
        {
            if (SelectedCard is null)
            {
                StatusMessage = "Select a card from the search results first.";
                return;
            }

            Draft.SetConditionCard(SelectedCard);
            StatusMessage = $"Condition card set to {SelectedCard.Name} ({SelectedCard.Id}).";
        }

        /// <summary>Validates and applies the current rule draft to the profile.</summary>
        public bool ApplyRule()
        {
            string ruleId = SelectedRule?.Id ?? Guid.NewGuid().ToString("N");
            if (!Draft.TryCreateRule(ruleId, out BotRule? rule, out string? inputError) || rule is null)
            {
                StatusMessage = inputError ?? "The rule draft is invalid.";
                return false;
            }

            BotProfile candidate = BuildProfile();
            int existingIndex = candidate.Rules.FindIndex(existing => existing.Id == rule.Id);
            if (existingIndex >= 0)
            {
                candidate.Rules[existingIndex] = rule;
            }
            else
            {
                candidate.Rules.Add(rule);
            }

            ValidationErrors = _profileService.Validate(candidate);
            OnPropertyChanged(nameof(ValidationErrors));
            if (ValidationErrors.Count > 0)
            {
                StatusMessage = "The rule was not applied. Correct the validation errors below.";
                return false;
            }

            if (existingIndex >= 0)
            {
                Rules[existingIndex] = rule;
            }
            else
            {
                Rules.Add(rule);
            }

            SortRulesByPriority();
            SelectedRule = rule;
            _isRuleDraftActive = true;
            StatusMessage = "Rule applied. Save the profile to write the JSON file.";
            return true;
        }

        /// <summary>Deletes the selected rule from the profile.</summary>
        public void RemoveSelectedRule()
        {
            if (SelectedRule is null)
            {
                return;
            }

            Rules.Remove(SelectedRule);
            SelectedRule = null;
            _isRuleDraftActive = false;
            BeginNewRule();
            StatusMessage = "Rule removed.";
        }

        /// <summary>Writes the current profile to a JSON file.</summary>
        public async Task SaveProfileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            if (_isRuleDraftActive && !ApplyRule())
            {
                throw new InvalidDataException(StatusMessage);
            }

            BotProfile profile = BuildProfile();
            ValidationErrors = _profileService.Validate(profile);
            OnPropertyChanged(nameof(ValidationErrors));
            if (ValidationErrors.Count > 0)
            {
                throw new InvalidDataException(string.Join(Environment.NewLine, ValidationErrors));
            }

            await _profileService.WriteFileAsync(profile, filePath, cancellationToken);
            FilePath = filePath;
            IsDirty = false;
            StatusMessage = $"Bot profile saved to {filePath}.";
        }

        /// <summary>Loads a valid profile, preserving the current profile on failure.</summary>
        public async Task LoadProfileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            BotProfile profile = await _profileService.ReadFileAsync(filePath, cancellationToken);
            LoadProfile(profile);
            FilePath = filePath;
            IsDirty = false;
            StatusMessage = $"Bot profile loaded from {filePath}.";
        }

        /// <summary>Displays a file operation error in the editor.</summary>
        public void ReportFileError(string message) =>
            StatusMessage = $"Bot profile file operation failed: {message}";

        private void LoadProfile(BotProfile profile)
        {
            _isLoading = true;
            BotName = profile.BotName;
            InitialModeId = profile.InitialModeId;
            ReplaceModes(profile.Modes);
            ReplaceCards(profile.Cards);
            ReplaceGroups(profile.Groups);
            ReplaceRules(profile.Rules);
            SortRulesByPriority();

            _isRuleDraftActive = false;
            SelectedRule = null;
            Draft.Reset(InitialModeId);
            ValidationErrors = Array.Empty<string>();
            OnPropertyChanged(nameof(ValidationErrors));
            _isLoading = false;
        }

        private void LoadRuleDraft(BotRule rule)
        {
            _isLoading = true;
            _isRuleDraftActive = true;
            Draft.Load(rule);
            _isLoading = false;
        }

        private BotProfile BuildProfile() => new()
        {
            SchemaVersion = BotProfile.CurrentSchemaVersion,
            BotName = BotName,
            InitialModeId = InitialModeId,
            Modes = Modes.ToList(),
            Cards = Cards.Select(CloneCardProfile).ToList(),
            Groups = Groups.Select(CloneCardGroup).ToList(),
            Rules = Rules.ToList()
        };

        private void AddModeToCollection(BotMode mode)
        {
            mode.PropertyChanged += OnModeChanged;
            Modes.Add(mode);
        }

        private void ReplaceModes(IEnumerable<BotMode> modes)
        {
            foreach (BotMode mode in Modes)
            {
                mode.PropertyChanged -= OnModeChanged;
            }

            SelectedMode = null;
            Modes.Clear();
            foreach (BotMode mode in modes)
            {
                AddModeToCollection(new BotMode { Id = mode.Id, Name = mode.Name });
            }

            SelectedMode = Modes.FirstOrDefault();
        }

        private void ReplaceRules(IEnumerable<BotRule> rules)
        {
            Rules.Clear();
            foreach (BotRule rule in rules)
            {
                Rules.Add(rule);
            }
        }

        private void ReplaceCards(IEnumerable<BotCardProfile>? cards)
        {
            Cards.Clear();
            if (cards is null)
            {
                return;
            }

            foreach (BotCardProfile card in cards)
            {
                Cards.Add(CloneCardProfile(card));
            }
        }

        private void ReplaceGroups(IEnumerable<BotCardGroup>? groups)
        {
            Groups.Clear();
            SelectedGroup = null;
            if (groups is null)
            {
                return;
            }

            foreach (BotCardGroup group in groups)
            {
                Groups.Add(CloneCardGroup(group));
            }

            SelectedGroup = Groups.FirstOrDefault();
        }

        private static BotCardProfile CloneCardProfile(BotCardProfile card) => new()
        {
            CardId = card.CardId,
            CardName = card.CardName,
            IsEnabled = card.IsEnabled,
            Notes = card.Notes,
            GroupIds = card.GroupIds?.ToList() ?? new List<string>()
        };

        private static BotCardGroup CloneCardGroup(BotCardGroup group) => new()
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CardIds = group.CardIds?.ToList() ?? new List<int>(),
            Priority = group.Priority
        };

        private void OnModeChanged(object? sender, PropertyChangedEventArgs e)
        {
            MarkDirty();
        }

        private void SortRulesByPriority()
        {
            BotRule[] sortedRules = Rules
                .OrderByDescending(rule => rule.Priority)
                .ToArray();

            for (int targetIndex = 0; targetIndex < sortedRules.Length; targetIndex++)
            {
                int currentIndex = Rules.IndexOf(sortedRules[targetIndex]);
                if (currentIndex != targetIndex)
                {
                    Rules.Move(currentIndex, targetIndex);
                }
            }
        }

        private void OnDraftChanged(object? sender, EventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            _isRuleDraftActive = true;
            MarkDirty();
        }

        private void MarkDirty()
        {
            if (_isLoading)
            {
                return;
            }

            IsDirty = true;
            ValidationErrors = Array.Empty<string>();
            OnPropertyChanged(nameof(ValidationErrors));
        }

    }
}
