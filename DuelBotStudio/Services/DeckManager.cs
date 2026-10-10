using DuelBotStudio.Models;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;

namespace DuelBotStudio.Services
{
    public class DeckManager
    {
        private readonly YgoCardInfoService _cardInfoService;

        public DeckModel CurrentDeck { get; set; }

        public DeckManager(YgoCardInfoService cardInfoService)
        {
            _cardInfoService = cardInfoService;
            CurrentDeck = new DeckModel();
        }

        public async Task LoadFromParseResultAsync(YdkParseResult parseResult)
        {
            CurrentDeck = new DeckModel();

            foreach (int id in parseResult.MainDeckIds)
            {
                CardModel? card = await _cardInfoService.GetCardByIdAsync(id) ?? new CardModel { Id = id, Name = $"Card {id}", Description = string.Empty, Type = "Unknown", Attribute = string.Empty, Race = string.Empty };
                CurrentDeck.MainDeck.Add(card);
            }

            foreach (int id in parseResult.ExtraDeckIds)
            {
                CardModel? card = await _cardInfoService.GetCardByIdAsync(id) ?? new CardModel { Id = id, Name = $"Card {id}", Description = string.Empty, Type = "Unknown", Attribute = string.Empty, Race = string.Empty };
                CurrentDeck.ExtraDeck.Add(card);
            }

            foreach (int id in parseResult.SideDeckIds)
            {
                CardModel? card = await _cardInfoService.GetCardByIdAsync(id) ?? new CardModel { Id = id, Name = $"Card {id}", Description = string.Empty, Type = "Unknown", Attribute = string.Empty, Race = string.Empty };
                CurrentDeck.SideDeck.Add(card);
            }
        }

        public bool IsExtraDeckCard(CardModel? card)
        {
            if (string.IsNullOrWhiteSpace(card?.Type))
            {
                return false;
            }

            string type = card.Type.ToLowerInvariant();
            return type.Contains("fusion", StringComparison.Ordinal) ||
                   type.Contains("synchro", StringComparison.Ordinal) ||
                   type.Contains("xyz", StringComparison.Ordinal) ||
                   type.Contains("link", StringComparison.Ordinal);
        }

        public void AddCard(CardModel card)
        {
            if (CurrentDeck == null)
            {
                CurrentDeck = new DeckModel();
            }

            if (IsExtraDeckCard(card))
            {
                CurrentDeck.ExtraDeck.Add(card);
            }
            else
            {
                CurrentDeck.MainDeck.Add(card);
            }
        }

        public void RemoveCardFromMain(int cardId)
        {
            var cardToRemove = CurrentDeck.MainDeck.FirstOrDefault(c => c.Id == cardId);
            if (cardToRemove != null)
            {
                CurrentDeck.MainDeck.Remove(cardToRemove);
            }
        }
        public void RemoveCardFromExtra(int cardId)
        {
            var cardToRemove = CurrentDeck.ExtraDeck.FirstOrDefault(c => c.Id == cardId);
            if (cardToRemove != null)
            {
                CurrentDeck.ExtraDeck.Remove(cardToRemove);
            }
        }

        public void RemoveCardFromSide(int cardId)
        {
            var cardToRemove = CurrentDeck.SideDeck.FirstOrDefault(c => c.Id == cardId);
            if (cardToRemove != null)
            {
                CurrentDeck.SideDeck.Remove(cardToRemove);
            }
        }
    }
}
