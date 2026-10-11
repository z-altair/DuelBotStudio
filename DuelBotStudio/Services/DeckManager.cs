using DuelBotStudio.Models;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;
using System.IO;
using System.Net.Http;

namespace DuelBotStudio.Services
{
    /// <summary>
    /// Owns the active deck and applies deck editing and validation rules.
    /// </summary>
    public sealed class DeckManager
    {
        private readonly YgoCardInfoService _cardInfoService;
        private DeckRules _rules;

        /// <summary>Creates a manager with default structural rules when none are supplied.</summary>
        /// <param name="cardInfoService">Service used to resolve imported card IDs.</param>
        /// <param name="rules">Optional structural limits for the active deck.</param>
        public DeckManager(YgoCardInfoService cardInfoService, DeckRules? rules = null)
        {
            _cardInfoService = cardInfoService ?? throw new ArgumentNullException(nameof(cardInfoService));
            _rules = rules ?? new DeckRules();
            CurrentDeck = new DeckModel();
        }

        /// <summary>Raised after the active deck is replaced or edited.</summary>
        public event EventHandler? DeckChanged;

        /// <summary>Raised after the active deck validation rules change.</summary>
        public event EventHandler? RulesChanged;

        /// <summary>Gets the current deck.</summary>
        public DeckModel CurrentDeck { get; private set; }

        /// <summary>Gets the structural rules used to validate the current deck.</summary>
        public DeckRules Rules => _rules;

        /// <summary>Replaces the structural rules used to validate the active deck.</summary>
        /// <param name="rules">New non-banlist structural limits.</param>
        public void UpdateRules(DeckRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            RulesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Loads all card data into a temporary deck and replaces the current deck only
        /// after every lookup completes successfully.
        /// </summary>
        /// <param name="parseResult">A parser result without errors.</param>
        /// <exception cref="ArgumentException"><paramref name="parseResult"/> contains errors.</exception>
        /// <exception cref="HttpRequestException">A card lookup fails.</exception>
        /// <exception cref="InvalidDataException">A parsed card ID is not present in the database.</exception>
        public async Task LoadFromParseResultAsync(YdkParseResult parseResult)
        {
            ArgumentNullException.ThrowIfNull(parseResult);
            if (parseResult.HasErrors)
            {
                throw new ArgumentException("Cannot load a deck with parser errors.", nameof(parseResult));
            }

            var candidateDeck = new DeckModel();
            foreach (int id in parseResult.MainDeckIds)
            {
                candidateDeck.MainDeck.Add(await ResolveCardAsync(id, parseResult));
            }

            foreach (int id in parseResult.ExtraDeckIds)
            {
                candidateDeck.ExtraDeck.Add(await ResolveCardAsync(id, parseResult));
            }

            foreach (int id in parseResult.SideDeckIds)
            {
                candidateDeck.SideDeck.Add(await ResolveCardAsync(id, parseResult));
            }

            CurrentDeck = candidateDeck;
            DeckChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Adds a card to the explicitly selected deck section.</summary>
        /// <param name="card">Card to add.</param>
        /// <param name="section">Destination section.</param>
        public void AddCard(CardModel card, DeckSection section)
        {
            ArgumentNullException.ThrowIfNull(card);
            GetSection(section).Add(card);
            DeckChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Removes the first matching card from the main deck.</summary>
        /// <param name="cardId">Identifier of the card to remove.</param>
        public void RemoveCardFromMain(int cardId) => RemoveCard(CurrentDeck.MainDeck, cardId);

        /// <summary>Removes the first matching card from the extra deck.</summary>
        /// <param name="cardId">Identifier of the card to remove.</param>
        public void RemoveCardFromExtra(int cardId) => RemoveCard(CurrentDeck.ExtraDeck, cardId);

        /// <summary>Removes the first matching card from the side deck.</summary>
        /// <param name="cardId">Identifier of the card to remove.</param>
        public void RemoveCardFromSide(int cardId) => RemoveCard(CurrentDeck.SideDeck, cardId);

        /// <summary>Validates section sizes and total copies against the configured rules.</summary>
        public DeckValidationResult Validate()
        {
            var errors = new List<string>();
            ValidateRange("Main deck", CurrentDeck.MainDeck.Count, _rules.MinimumMainDeckSize, _rules.MaximumMainDeckSize, errors);
            ValidateRange("Extra deck", CurrentDeck.ExtraDeck.Count, 0, _rules.MaximumExtraDeckSize, errors);
            ValidateRange("Side deck", CurrentDeck.SideDeck.Count, 0, _rules.MaximumSideDeckSize, errors);

            var excessiveCopies = CurrentDeck.MainDeck
                .Concat(CurrentDeck.ExtraDeck)
                .Concat(CurrentDeck.SideDeck)
                .GroupBy(card => card.Id)
                .Where(group => group.Count() > _rules.MaximumCopiesPerCard);

            foreach (var group in excessiveCopies)
            {
                errors.Add($"Card {group.Key} has {group.Count()} copies; the configured limit is {_rules.MaximumCopiesPerCard}.");
            }

            return new DeckValidationResult(errors);
        }

        private async Task<CardModel> ResolveCardAsync(int id, YdkParseResult parseResult)
        {
            CardModel? card = await _cardInfoService.GetCardByIdAsync(id);
            if (card is not null)
            {
                return card;
            }

            string lineInformation = parseResult.CardIdLineNumbers.TryGetValue(id, out var lines)
                ? $" on line(s) {string.Join(", ", lines)}"
                : string.Empty;
            throw new InvalidDataException($"Card ID {id}{lineInformation} was not found in the card database.");
        }

        private List<CardModel> GetSection(DeckSection section) => section switch
        {
            DeckSection.Main => CurrentDeck.MainDeck,
            DeckSection.Extra => CurrentDeck.ExtraDeck,
            DeckSection.Side => CurrentDeck.SideDeck,
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown deck section.")
        };

        private void RemoveCard(List<CardModel> section, int cardId)
        {
            var card = section.FirstOrDefault(candidate => candidate.Id == cardId);
            if (card is null)
            {
                return;
            }

            section.Remove(card);
            DeckChanged?.Invoke(this, EventArgs.Empty);
        }

        private static void ValidateRange(string section, int count, int minimum, int maximum, ICollection<string> errors)
        {
            if (count < minimum || count > maximum)
            {
                errors.Add($"{section} contains {count} cards; the configured range is {minimum}-{maximum}.");
            }
        }
    }
}
