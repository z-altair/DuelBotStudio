using DuelBotStudio.Services;
using System.Windows.Controls;

namespace DuelBotStudio.Views.Controls
{
    public partial class CardDetailsView : UserControl
    {
        private YgoCardInfoService? _cardInfoService;
        private int? _currentlySelectedCardId;

        public CardDetailsView()
        {
            InitializeComponent();
        }

        public void Initialize(YgoCardInfoService cardInfoService)
        {
            _cardInfoService = cardInfoService;
        }

        public void Clear()
        {
            _currentlySelectedCardId = null;
            SelectedCardImage.Source = null;
            SelectedCardName.Text = string.Empty;
            SelectedCardType.Text = string.Empty;
            SelectedCardStats.Text = string.Empty;
            SelectedCardDescription.Text = string.Empty;
        }

        public async Task ShowCardAsync(int cardId)
        {
            if (_cardInfoService == null || _currentlySelectedCardId == cardId)
            {
                return;
            }

            _currentlySelectedCardId = cardId;
            SelectedCardName.Text = "Loading details";
            SelectedCardType.Text = string.Empty;
            SelectedCardStats.Text = string.Empty;
            SelectedCardDescription.Text = string.Empty;

            var card = await _cardInfoService.getCardByIdAsync(cardId);
            if (_currentlySelectedCardId != cardId)
            {
                return;
            }

            if (card == null)
            {
                SelectedCardName.Text = "Failed to load card info.";
                return;
            }

            var image = await CardImageCache.getCardImageAsync(cardId);
            if (_currentlySelectedCardId != cardId)
            {
                return;
            }

            SelectedCardImage.Source = image;
            SelectedCardName.Text = card.name;
            SelectedCardType.Text = $"{card.type} / {card.race}";
            SelectedCardStats.Text = card.atk.HasValue || card.def.HasValue
                ? $"ATK: {card.atk?.ToString() ?? "?"} / DEF: {card.def?.ToString() ?? "?"}" +
                  (card.level.HasValue ? $" | Lvl/Rank: {card.level}" : string.Empty)
                : string.Empty;
            SelectedCardDescription.Text = card.description;
        }
    }
}
