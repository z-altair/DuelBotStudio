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

        private void OnLoadDeckButtonClick(object sender, RoutedEventArgs e)
        {
            LoadDeckRequested?.Invoke(this, e);
        }

        public async Task RefreshAsync()
        {
            if (_deckManager == null)
            {
                return;
            }

            var deck = _deckManager.CurrentDeck;
            await RenderSection(MainDeckPanel, MainDeckHeader, "Main Deck", deck.MainDeck);
            await RenderSection(ExtraDeckPanel, ExtraDeckHeader, "Extra Deck", deck.ExtraDeck);
            await RenderSection(SideDeckPanel, SideDeckHeader, "Side Deck", deck.SideDeck);
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

                BitmapImage? bitmap = await CardImageCache.GetCardImageAsync(card.Id);
                if (bitmap != null && panel.Children.Contains(image))
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

        private async void OnCardImageRightClick(object sender, MouseButtonEventArgs e)
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

            await RefreshAsync();
            e.Handled = true;
        }
    }
}
