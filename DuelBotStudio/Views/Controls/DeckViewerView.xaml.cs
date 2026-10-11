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
        private CancellationTokenSource? _refreshCancellation;

        /// <summary>Raised when the user selects a card.</summary>
        public event EventHandler<int>? CardSelected;

        /// <summary>Raised when the user requests a deck file import.</summary>
        public event RoutedEventHandler? LoadDeckRequested;

        /// <summary>Raised when the user requests saving the active deck.</summary>
        public event RoutedEventHandler? SaveDeckRequested;

        /// <summary>Creates the deck viewer control.</summary>
        public DeckViewerView()
        {
            InitializeComponent();
        }

        /// <summary>Connects the viewer to the active deck manager.</summary>
        public void Initialize(DeckManager deckManager, CardImageCache cardImageCache)
        {
            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();
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

            var cancellation = new CancellationTokenSource();
            CancellationTokenSource? previousRefresh = _refreshCancellation;
            _refreshCancellation = cancellation;
            previousRefresh?.Cancel();
            previousRefresh?.Dispose();
            CancellationToken token = cancellation.Token;
            var deck = _deckManager.CurrentDeck;
            StatusText.Text = string.Empty;
            try
            {
                await RenderSection(MainDeckPanel, MainDeckHeader, "Main Deck", deck.MainDeck, token);
                await RenderSection(ExtraDeckPanel, ExtraDeckHeader, "Extra Deck", deck.ExtraDeck, token);
                await RenderSection(SideDeckPanel, SideDeckHeader, "Side Deck", deck.SideDeck, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (HttpRequestException exception)
            {
                if (!token.IsCancellationRequested)
                {
                    StatusText.Text = $"Some card images could not be loaded: {exception.Message}";
                }
            }
            catch (IOException exception)
            {
                if (!token.IsCancellationRequested)
                {
                    StatusText.Text = $"The image cache could not be accessed: {exception.Message}";
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                if (!token.IsCancellationRequested)
                {
                    StatusText.Text = $"Image cache access was denied: {exception.Message}";
                }
            }
            finally
            {
                if (ReferenceEquals(_refreshCancellation, cancellation))
                {
                    _refreshCancellation = null;
                    cancellation.Dispose();
                }
            }
        }

        private async Task RenderSection(
            WrapPanel panel,
            TextBlock header,
            string title,
            IReadOnlyList<CardModel> cards,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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

                Task<BitmapImage> imageLoad = _cardImageCache.GetCardImageAsync(card.Id);
                BitmapImage bitmap;
                try
                {
                    bitmap = await imageLoad.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _ = imageLoad.ContinueWith(
                        task => System.Diagnostics.Trace.TraceError(
                            "Card image request for {0} failed after its deck refresh was cancelled: {1}",
                            card.Id,
                            task.Exception),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                    throw;
                }

                cancellationToken.ThrowIfCancellationRequested();
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

        private void OnSaveDeckButtonClick(object sender, RoutedEventArgs e)
        {
            SaveDeckRequested?.Invoke(this, e);
        }
    }
}
