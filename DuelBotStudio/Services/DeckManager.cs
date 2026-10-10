using DuelBotStudio.Models;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;

namespace DuelBotStudio.Services
{
    public class DeckManager
    {
        private readonly YgoCardInfoService _cardInfoService;

        public DeckModel currentDeck { get; set; }

        public DeckManager(YgoCardInfoService cardInfoService)
        {
            _cardInfoService = cardInfoService;
            currentDeck = new DeckModel();
        }

        public async Task loadFromParseResultAsync(YdkParseResult parseResult)
        {
            currentDeck = new DeckModel();

            foreach (int id in parseResult.mainDeckIds)
            {
                CardModel? card = await _cardInfoService.getCardByIdAsync(id) ?? new CardModel { id = id, name = $"Card {id}", description = string.Empty, type = "Unknown", attribute = string.Empty, race = string.Empty };
                currentDeck.mainDeck.Add(card);
            }

            foreach (int id in parseResult.extraDeckIds)
            {
                CardModel? card = await _cardInfoService.getCardByIdAsync(id) ?? new CardModel { id = id, name = $"Card {id}", description = string.Empty, type = "Unknown", attribute = string.Empty, race = string.Empty };
                currentDeck.extraDeck.Add(card);
            }

            foreach (int id in parseResult.sideDeckIds)
            {
                CardModel? card = await _cardInfoService.getCardByIdAsync(id) ?? new CardModel { id = id, name = $"Card {id}", description = string.Empty, type = "Unknown", attribute = string.Empty, race = string.Empty };
                currentDeck.sideDeck.Add(card);
            }
        }

        public bool isExtraDeckCard(CardModel? card)
        {
            if (string.IsNullOrWhiteSpace(card?.type))
            {
                return false;
            }

            string type = card.type.ToLowerInvariant();
            return type.Contains("fusion", StringComparison.Ordinal) ||
                   type.Contains("synchro", StringComparison.Ordinal) ||
                   type.Contains("xyz", StringComparison.Ordinal) ||
                   type.Contains("link", StringComparison.Ordinal);
        }

        public void addCard(CardModel card)
        {
            if (currentDeck == null)
            {
                currentDeck = new DeckModel();
            }

            if (isExtraDeckCard(card))
            {
                currentDeck.extraDeck.Add(card);
            }
            else
            {
                currentDeck.mainDeck.Add(card);
            }
        }

        public void removeCardFromMain(int cardId)
        {
            var cardToRemove = currentDeck.mainDeck.FirstOrDefault(c => c.id == cardId);
            if (cardToRemove != null)
            {
                currentDeck.mainDeck.Remove(cardToRemove);
            }
        }
        public void removeCardFromExtra(int cardId)
        {
            var cardToRemove = currentDeck.extraDeck.FirstOrDefault(c => c.id == cardId);
            if (cardToRemove != null)
            {
                currentDeck.extraDeck.Remove(cardToRemove);
            }
        }

        public void removeCardFromSide(int cardId)
        {
            var cardToRemove = currentDeck.sideDeck.FirstOrDefault(c => c.id == cardId);
            if (cardToRemove != null)
            {
                currentDeck.sideDeck.Remove(cardToRemove);
            }
        }
    }
}
