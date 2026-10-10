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

            var card = await _cardInfoService.GetCardByIdAsync(cardId);
            if (_currentlySelectedCardId != cardId)
            {
                return;
            }

            if (card == null)
            {
                SelectedCardName.Text = "Failed to load card info.";
                return;
            }

            var image = await CardImageCache.GetCardImageAsync(cardId);
            if (_currentlySelectedCardId != cardId)
            {
                return;
            }

            SelectedCardImage.Source = image;
            SelectedCardName.Text = card.Name;
            SelectedCardType.Text = $"{card.Type} / {card.Race}";
            SelectedCardStats.Text = card.Atk.HasValue || card.Def.HasValue
                ? $"ATK: {card.Atk?.ToString() ?? "?"} / DEF: {card.Def?.ToString() ?? "?"}" +
                  (card.Level.HasValue ? $" | Lvl/Rank: {card.Level}" : string.Empty)
                : string.Empty;
            SelectedCardDescription.Text = card.Description;
        }
    }
}
