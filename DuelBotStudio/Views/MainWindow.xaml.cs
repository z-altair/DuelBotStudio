using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;
using DuelBotStudio.Services;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DuelBotStudio
{
    public partial class MainWindow : Window
    {
        private readonly DeckManager _deckManager;
        private readonly YdkParserService _ydkParserService;
        private readonly YgoCardInfoService _cardInfoService;
        private int? _currentlySelectedCardId;
        public MainWindow()
        {
            InitializeComponent();
            _ydkParserService = new YdkParserService();
            _cardInfoService = new YgoCardInfoService();
            _deckManager = new DeckManager(_cardInfoService);
        }

        private async void onLoadDeckButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select your deck.",
                Filter = "Deck files (*.ydk)|*.ydk",
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var parseResult = _ydkParserService.ParseFile(dialog.FileName);

            if (parseResult.hasErrors)
            {
                MessageBox.Show(string.Join("\n", parseResult.errors), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _deckManager.loadFromParseResultAsync(parseResult);

            _currentlySelectedCardId = null;

            await refreshAllSectionsAsync();

        }

        private async Task renderSection(WrapPanel panel, TextBlock header, string title, IReadOnlyList<CardModel> cards)
        {
            panel.Children.Clear();
            var cardList = cards ?? Array.Empty<CardModel>();
            header.Text = $"{title} ({cardList.Count})";

            foreach (var card in cardList)
            {
                int cardId = card.id;
                Image image = new Image
                {
                    Width = 100,
                    Height = 146,
                    Margin = new Thickness(5),
                    Stretch = Stretch.Uniform,
                    Tag = cardId,
                    Cursor = Cursors.Hand
                };

                image.MouseLeftButtonDown += onCardImageClick;
                image.MouseRightButtonDown += onCardImageRightClick;
                panel.Children.Add(image);

                BitmapImage? bitmap = await CardImageCache.getCardImageAsync(cardId);
                if (bitmap != null)
                {
                    image.Source = bitmap;
                }
            }
        }

        private async Task refreshAllSectionsAsync()
        {
            if (_deckManager.currentDeck == null)
            {
                _deckManager.currentDeck = new DeckModel();
            }

            await renderSection(MainDeckPanel, MainDeckHeader, "Main Deck", _deckManager.currentDeck.mainDeck);
            await renderSection(ExtraDeckPanel, ExtraDeckHeader, "Extra Deck", _deckManager.currentDeck.extraDeck);
            await renderSection(SideDeckPanel, SideDeckHeader, "Side Deck", _deckManager.currentDeck.sideDeck);
        }
            
        private async void onCardImageClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Tag is int cardId)
            {
                if (_currentlySelectedCardId == cardId)
                {
                    return;
                }

                _currentlySelectedCardId = cardId;

                SelectedCardName.Text = "Loading details";
                SelectedCardType.Text = string.Empty;
                SelectedCardStats.Text = string.Empty;
                SelectedCardDescription.Text = string.Empty;

                CardModel? card = await _cardInfoService.getCardByIdAsync(cardId);

                if (_currentlySelectedCardId != cardId)
                {
                    return;
                }

                if (card != null)
                {
                    SelectedCardImage.Source = await CardImageCache.getCardImageAsync(cardId);
                    SelectedCardName.Text = card.name;
                    SelectedCardType.Text = $"{card.type} / {card.race}";

                    if (card.atk.HasValue || card.def.HasValue)
                    {
                        string levelText = card.level.HasValue ? $" | Lvl/Rank: {card.level}" : string.Empty;
                        SelectedCardStats.Text = $"ATK: {card.atk?.ToString() ?? "?"} / DEF: {card.def?.ToString() ?? "?"}{levelText}";
                    }
                    else
                    {
                        SelectedCardStats.Text = string.Empty;
                    }

                    SelectedCardDescription.Text = card.description;
                }
                else
                {
                    SelectedCardName.Text = "Failed to load card info.";
                }
            }
        }

        private async void onCardImageRightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Tag is int cardId && image.Parent is WrapPanel panel)
            {
                if (panel == MainDeckPanel) _deckManager.removeCardFromMain(cardId);
                else if (panel == ExtraDeckPanel) _deckManager.removeCardFromExtra(cardId);
                else if (panel == SideDeckPanel) _deckManager.removeCardFromSide(cardId);

                await refreshAllSectionsAsync();
            }
        }

        private async void onSearchButtonClick(object sender, RoutedEventArgs e)
        {
            string query = SearchInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                SearchResultsListBox.ItemsSource = null;
                return;
            }

            var results = await _cardInfoService.searchCardsByNameAsync(query);

            SearchResultsListBox.ItemsSource = results;
            SearchResultsListBox.DisplayMemberPath = "name";
        }

        private async void onSearchResultRightClick(object sender, MouseButtonEventArgs e)
        {
            CardModel? selectedCard = SearchResultsListBox.SelectedItem as CardModel;
            if (selectedCard == null)
            {
                return;
            }

            _deckManager.addCard(selectedCard);
            await refreshAllSectionsAsync();
            e.Handled = true;
        }
    }
}