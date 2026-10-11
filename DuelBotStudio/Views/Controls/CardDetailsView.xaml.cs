using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Controls;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Services;

namespace DuelBotStudio.Views.Controls
{
    /// <summary>Displays details and imagery for the currently selected card.</summary>
    public partial class CardDetailsView : UserControl
    {
        private YgoCardInfoService? _cardInfoService;
        private CardImageCache? _cardImageCache;
        private int? _currentlySelectedCardId;

        /// <summary>Creates the card details control.</summary>
        public CardDetailsView()
        {
            InitializeComponent();
        }

        /// <summary>Sets the service used to retrieve card metadata.</summary>
        public void Initialize(YgoCardInfoService cardInfoService, CardImageCache cardImageCache)
        {
            _cardInfoService = cardInfoService;
            _cardImageCache = cardImageCache;
        }

        /// <summary>Clears the selected card and its displayed details.</summary>
        public void Clear()
        {
            _currentlySelectedCardId = null;
            SelectedCardImage.Source = null;
            SelectedCardStatus.Text = string.Empty;
            SelectedCardName.Text = string.Empty;
            SelectedCardType.Text = string.Empty;
            SelectedCardStats.Text = string.Empty;
            SelectedCardDescription.Text = string.Empty;
        }

        /// <summary>Loads card data and updates the details view.</summary>
        public async Task ShowCardAsync(int cardId)
        {
            if (_cardInfoService is null || _cardImageCache is null || _currentlySelectedCardId == cardId)
            {
                return;
            }

            _currentlySelectedCardId = cardId;
            SelectedCardStatus.Text = string.Empty;
            SelectedCardName.Text = "Loading details";
            SelectedCardType.Text = string.Empty;
            SelectedCardStats.Text = string.Empty;
            SelectedCardDescription.Text = string.Empty;

            CardModel? card;
            try
            {
                card = await _cardInfoService.GetCardByIdAsync(cardId);
            }
            catch (HttpRequestException exception)
            {
                ShowError(cardId, $"Card details could not be loaded: {exception.Message}");
                return;
            }
            catch (IOException exception)
            {
                ShowError(cardId, $"Card cache could not be accessed: {exception.Message}");
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowError(cardId, $"Card cache access was denied: {exception.Message}");
                return;
            }
            catch (JsonException exception)
            {
                ShowError(cardId, $"Card service data could not be read: {exception.Message}");
                return;
            }

            if (_currentlySelectedCardId != cardId)
            {
                return;
            }

            if (card is null)
            {
                _currentlySelectedCardId = null;
                SelectedCardName.Text = "Card was not found.";
                return;
            }

            System.Windows.Media.Imaging.BitmapImage? image = null;
            try
            {
                image = await _cardImageCache.GetCardImageAsync(cardId);
            }
            catch (HttpRequestException exception)
            {
                SelectedCardStatus.Text = $"Card image could not be loaded: {exception.Message}";
            }
            catch (IOException exception)
            {
                SelectedCardStatus.Text = $"Card image cache could not be accessed: {exception.Message}";
            }
            catch (UnauthorizedAccessException exception)
            {
                SelectedCardStatus.Text = $"Card image cache access was denied: {exception.Message}";
            }

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

        private void ShowError(int cardId, string message)
        {
            if (_currentlySelectedCardId != cardId)
            {
                return;
            }

            _currentlySelectedCardId = null;
            SelectedCardImage.Source = null;
            SelectedCardStatus.Text = message;
            SelectedCardName.Text = "Unable to load card details.";
        }
    }
}
