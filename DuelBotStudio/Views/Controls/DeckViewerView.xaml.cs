using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;
using DuelBotStudio.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DuelBotStudio.Views.Controls
{
    public partial class DeckViewerView : UserControl
    {
        private DeckManager? _deckManager;

        public event EventHandler<int>? CardSelected;
        public event RoutedEventHandler? LoadDeckRequested;

        public DeckViewerView()
        {
            InitializeComponent();
        }

        public void Initialize(DeckManager deckManager)
        {
            _deckManager = deckManager;
        }

        private void onLoadDeckButtonClick(object sender, RoutedEventArgs e)
        {
            LoadDeckRequested?.Invoke(this, e);
        }

        public async Task RefreshAsync()
        {
            if (_deckManager == null)
            {
                return;
            }

            var deck = _deckManager.currentDeck;
            await renderSection(MainDeckPanel, MainDeckHeader, "Main Deck", deck.mainDeck);
            await renderSection(ExtraDeckPanel, ExtraDeckHeader, "Extra Deck", deck.extraDeck);
            await renderSection(SideDeckPanel, SideDeckHeader, "Side Deck", deck.sideDeck);
        }

        private async Task renderSection(WrapPanel panel, TextBlock header, string title, IReadOnlyList<CardModel> cards)
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
                image.MouseLeftButtonDown += onCardImageClick;
                image.MouseRightButtonDown += onCardImageRightClick;
                panel.Children.Add(image);

                BitmapImage? bitmap = await CardImageCache.getCardImageAsync(card.id);
                if (bitmap != null && panel.Children.Contains(image))
                {
                    image.Source = bitmap;
                }
            }
        }

        private void onCardImageClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image { Tag: CardModel card })
            {
                CardSelected?.Invoke(this, card.id);
            }
        }

        private async void onCardImageRightClick(object sender, MouseButtonEventArgs e)
        {
            if (_deckManager == null || sender is not Image image || image.Tag is not CardModel card ||
                image.Parent is not WrapPanel panel)
            {
                return;
            }

            if (panel == MainDeckPanel)
            {
                _deckManager.removeCardFromMain(card.id);
            }
            else if (panel == ExtraDeckPanel)
            {
                _deckManager.removeCardFromExtra(card.id);
            }
            else if (panel == SideDeckPanel)
            {
                _deckManager.removeCardFromSide(card.id);
            }

            await RefreshAsync();
            e.Handled = true;
        }
    }
}
