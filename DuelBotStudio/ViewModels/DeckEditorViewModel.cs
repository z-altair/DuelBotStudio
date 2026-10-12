using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Globalization;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;
using DuelBotStudio.Services;

namespace DuelBotStudio.ViewModels
{
    /// <summary>
    /// Provides searchable card results and deck editing state to the WPF deck editor.
    /// </summary>
    public sealed class DeckEditorViewModel : ObservableObject
    {
        private readonly YgoCardInfoService _cardInfoService;
        private readonly DeckManager _deckManager;
        private int _searchGeneration;
        private string _searchQuery = string.Empty;
        private string _statusMessage = string.Empty;
        private string _minimumMainDeckSize;
        private string _maximumMainDeckSize;
        private string _maximumExtraDeckSize;
        private string _maximumSideDeckSize;
        private string _maximumCopiesPerCard;
        private bool _isBusy;
        private CardModel? _selectedCard;

        /// <summary>Creates the editor view model.</summary>
        public DeckEditorViewModel(YgoCardInfoService cardInfoService, DeckManager deckManager)
        {
            _cardInfoService = cardInfoService;
            _deckManager = deckManager;
            _deckManager.DeckChanged += OnDeckChanged;
            _deckManager.RulesChanged += OnRulesChanged;
            _minimumMainDeckSize = deckManager.Rules.MinimumMainDeckSize.ToString(CultureInfo.InvariantCulture);
            _maximumMainDeckSize = deckManager.Rules.MaximumMainDeckSize.ToString(CultureInfo.InvariantCulture);
            _maximumExtraDeckSize = deckManager.Rules.MaximumExtraDeckSize.ToString(CultureInfo.InvariantCulture);
            _maximumSideDeckSize = deckManager.Rules.MaximumSideDeckSize.ToString(CultureInfo.InvariantCulture);
            _maximumCopiesPerCard = deckManager.Rules.MaximumCopiesPerCard.ToString(CultureInfo.InvariantCulture);
            RefreshValidation();
        }

        /// <summary>Gets the current search query.</summary>
        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        /// <summary>Gets the current card search results.</summary>
        public ObservableCollection<CardModel> SearchResults { get; } = new();

        /// <summary>Gets or sets the selected search result.</summary>
        public CardModel? SelectedCard
        {
            get => _selectedCard;
            set
            {
                if (SetProperty(ref _selectedCard, value))
                {
                    OnPropertyChanged(nameof(CanAddSelectedCard));
                }
            }
        }

        /// <summary>Gets whether a search result is selected.</summary>
        public bool CanAddSelectedCard => SelectedCard is not null && !IsBusy;

        /// <summary>Gets whether the search action is available.</summary>
        public bool CanSearch => !IsBusy;

        /// <summary>Gets or sets the minimum allowed main-deck size.</summary>
        public string MinimumMainDeckSize
        {
            get => _minimumMainDeckSize;
            set => SetProperty(ref _minimumMainDeckSize, value);
        }

        /// <summary>Gets or sets the maximum allowed main-deck size.</summary>
        public string MaximumMainDeckSize
        {
            get => _maximumMainDeckSize;
            set => SetProperty(ref _maximumMainDeckSize, value);
        }

        /// <summary>Gets or sets the maximum allowed extra-deck size.</summary>
        public string MaximumExtraDeckSize
        {
            get => _maximumExtraDeckSize;
            set => SetProperty(ref _maximumExtraDeckSize, value);
        }

        /// <summary>Gets or sets the maximum allowed side-deck size.</summary>
        public string MaximumSideDeckSize
        {
            get => _maximumSideDeckSize;
            set => SetProperty(ref _maximumSideDeckSize, value);
        }

        /// <summary>Gets or sets the maximum copies of each card across all sections.</summary>
        public string MaximumCopiesPerCard
        {
            get => _maximumCopiesPerCard;
            set => SetProperty(ref _maximumCopiesPerCard, value);
        }

        /// <summary>Gets the current deck validation errors.</summary>
        public IReadOnlyList<string> ValidationErrors { get; private set; } = Array.Empty<string>();

        /// <summary>Gets a positive validation message when the deck has no errors.</summary>
        public string ValidationStatus => ValidationErrors.Count == 0 ? "Deck meets configured limits." : string.Empty;

        /// <summary>Gets a user-facing status or service error.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>Gets whether a card search is in progress.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanAddSelectedCard));
                    OnPropertyChanged(nameof(CanSearch));
                }
            }
        }

        /// <summary>Searches by card name and description, reporting service errors to the view.</summary>
        public async Task SearchAsync()
        {
            int generation = ++_searchGeneration;
            if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Trim().Length < 3)
            {
                SearchResults.Clear();
                SelectedCard = null;
                IsBusy = false;
                StatusMessage = "Enter at least three characters to search.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Searching...";
            SearchResults.Clear();
            SelectedCard = null;

            try
            {
                var results = await _cardInfoService.SearchCardsByNameAsync(SearchQuery.Trim());
                if (generation != _searchGeneration)
                {
                    return;
                }

                SearchResults.Clear();
                foreach (var card in results)
                {
                    SearchResults.Add(card);
                }

                SelectedCard = null;
                StatusMessage = results.Count == 0 ? "No matching cards were found." : $"{results.Count} card(s) found.";
            }
            catch (HttpRequestException exception)
            {
                if (generation == _searchGeneration)
                {
                    StatusMessage = $"Card search failed: {exception.Message}";
                }
            }
            catch (System.Text.Json.JsonException exception)
            {
                if (generation == _searchGeneration)
                {
                    StatusMessage = $"The card service returned invalid data: {exception.Message}";
                }
            }
            finally
            {
                if (generation == _searchGeneration)
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>Adds a card to the specified section and refreshes validation.</summary>
        public void AddCard(CardModel card, DeckSection section)
        {
            ArgumentNullException.ThrowIfNull(card);
            try
            {
                _deckManager.AddCard(card, section);
                StatusMessage = $"{card.Name} added to the {section} deck.";
            }
            catch (ArgumentException exception)
            {
                StatusMessage = exception.Message;
            }
        }

        /// <summary>Adds the selected card to main or extra based on its card type.</summary>
        public void AddSelectedCardAutomatically()
        {
            if (SelectedCard is null)
            {
                StatusMessage = "Select a card before adding it to the deck.";
                return;
            }

            DeckSection section = DeckManager.IsExtraDeckCard(SelectedCard) ? DeckSection.Extra : DeckSection.Main;
            AddCard(SelectedCard, section);
        }

        /// <summary>Applies configured section-size and copy limits to the active deck.</summary>
        public void ApplyRules()
        {
            if (!TryParseRule(MinimumMainDeckSize, out int minimumMain) ||
                !TryParseRule(MaximumMainDeckSize, out int maximumMain) ||
                !TryParseRule(MaximumExtraDeckSize, out int maximumExtra) ||
                !TryParseRule(MaximumSideDeckSize, out int maximumSide) ||
                !TryParseRule(MaximumCopiesPerCard, out int maximumCopies))
            {
                StatusMessage = "Deck limits must be whole numbers greater than or equal to zero.";
                return;
            }

            try
            {
                _deckManager.UpdateRules(new DeckRules(
                    minimumMain,
                    maximumMain,
                    maximumExtra,
                    maximumSide,
                    maximumCopies));
                StatusMessage = "Deck limits applied.";
            }
            catch (ArgumentOutOfRangeException exception)
            {
                StatusMessage = $"Deck limits are invalid: {exception.Message}";
            }
        }

        private void OnDeckChanged(object? sender, EventArgs e) => RefreshValidation();

        private void OnRulesChanged(object? sender, EventArgs e) => RefreshValidation();

        private static bool TryParseRule(string value, out int result) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);

        private void RefreshValidation()
        {
            ValidationErrors = _deckManager.Validate().Errors;
            OnPropertyChanged(nameof(ValidationErrors));
            OnPropertyChanged(nameof(ValidationStatus));
        }

    }
}
