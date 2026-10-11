using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;
using DuelBotStudio.Services;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DuelBotStudio.Views.Controls
{
    /// <summary>Displays deck sections and supports card selection and removal.</summary>
    public partial class DeckViewerView : UserControl
    {
        private DeckManager? _deckManager;
        private CardImageCache? _cardImageCache;

        /// <summary>Raised when the user selects a card.</summary>
        public event EventHandler<int>? CardSelected;

        /// <summary>Raised when the user requests a deck file import.</summary>
        public event RoutedEventHandler? LoadDeckRequested;

        /// <summary>Creates the deck viewer control.</summary>
        public DeckViewerView()
        {
            InitializeComponent();
        }

        /// <summary>Connects the viewer to the active deck manager.</summary>
        public void Initialize(DeckManager deckManager, CardImageCache cardImageCache)
        {
            if (_deckManager is not null)
            {
                _deckManager.DeckChanged -= OnDeckChanged;
            }

            _deckManager = deckManager;
            _cardImageCache = cardImageCache;
            _deckManager.DeckChanged += OnDeckChanged;
            _ = RefreshAsync();
        }

        private void OnLoadDeckButtonClick(object sender, RoutedEventArgs e)
        {
            LoadDeckRequested?.Invoke(this, e);
        }

        /// <summary>Rebuilds the displayed sections from the active deck.</summary>
        public async Task RefreshAsync()
        {
            if (_deckManager == null)
            {
                return;
            }

            var deck = _deckManager.CurrentDeck;
            StatusText.Text = string.Empty;
            try
            {
                await RenderSection(MainDeckPanel, MainDeckHeader, "Main Deck", deck.MainDeck);
                await RenderSection(ExtraDeckPanel, ExtraDeckHeader, "Extra Deck", deck.ExtraDeck);
                await RenderSection(SideDeckPanel, SideDeckHeader, "Side Deck", deck.SideDeck);
            }
            catch (HttpRequestException exception)
            {
                StatusText.Text = $"Some card images could not be loaded: {exception.Message}";
            }
            catch (IOException exception)
            {
                StatusText.Text = $"The image cache could not be accessed: {exception.Message}";
            }
            catch (UnauthorizedAccessException exception)
            {
                StatusText.Text = $"Image cache access was denied: {exception.Message}";
            }
        }

        private async Task RenderSection(WrapPanel panel, TextBlock header, string title, IReadOnlyList<CardModel> cards)
        {
            panel.Children.Clear();
            header.Text = $"{title} ({cards.Count})";

            foreach (var card in cards)
            {
                var image = new Image
                {
                    Width = 100,
                    Height = 146,
                    Margin = new Thickness(5),
                    Stretch = Stretch.Uniform,
                    Tag = card,
                    Cursor = Cursors.Hand
                };
                image.MouseLeftButtonDown += OnCardImageClick;
                image.MouseRightButtonDown += OnCardImageRightClick;
                panel.Children.Add(image);

                if (_cardImageCache is null)
                {
                    return;
                }

                BitmapImage bitmap = await _cardImageCache.GetCardImageAsync(card.Id);
                if (panel.Children.Contains(image))
                {
                    image.Source = bitmap;
                }
            }
        }

        private void OnCardImageClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image { Tag: CardModel card })
            {
                CardSelected?.Invoke(this, card.Id);
            }
        }

        private void OnCardImageRightClick(object sender, MouseButtonEventArgs e)
        {
            if (_deckManager == null || sender is not Image image || image.Tag is not CardModel card ||
                image.Parent is not WrapPanel panel)
            {
                return;
            }

            if (panel == MainDeckPanel)
            {
                _deckManager.RemoveCardFromMain(card.Id);
            }
            else if (panel == ExtraDeckPanel)
            {
                _deckManager.RemoveCardFromExtra(card.Id);
            }
            else if (panel == SideDeckPanel)
            {
                _deckManager.RemoveCardFromSide(card.Id);
            }

            e.Handled = true;
        }

        private async void OnDeckChanged(object? sender, EventArgs e)
        {
            await RefreshAsync();
        }
    }
}
