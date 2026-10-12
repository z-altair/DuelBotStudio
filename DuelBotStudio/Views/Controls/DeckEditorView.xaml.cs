using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Net.Http;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;

namespace DuelBotStudio.Views.Controls
{
    /// <summary>
    /// Presents card search, direct deck gestures, and validation feedback.
    /// </summary>
    public partial class DeckEditorView : UserControl
    {
        private DeckEditorViewModel? _viewModel;
        private CardImageCache? _cardImageCache;
        private Point _dragStartPoint;
        private CardModel? _dragCard;

        /// <summary>Raised when the pointer enters a search result.</summary>
        public event EventHandler<int>? CardHovered;

        /// <summary>Creates the deck editor view.</summary>
        public DeckEditorView()
        {
            InitializeComponent();
        }

        /// <summary>Connects the view to its services and active deck manager.</summary>
        public void Initialize(YgoCardInfoService cardInfoService, DeckManager deckManager, CardImageCache cardImageCache)
        {
            _viewModel = new DeckEditorViewModel(cardInfoService, deckManager);
            _cardImageCache = cardImageCache;
            DataContext = _viewModel;
        }

        private async void OnSearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel is not null)
            {
                await _viewModel.SearchAsync();
            }
        }

        private async void OnSearchQueryKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel is not null)
            {
                e.Handled = true;
                await _viewModel.SearchAsync();
            }
        }

        private void OnSearchResultMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TryGetCard(e.OriginalSource, out CardModel? card))
            {
                if (_viewModel is not null)
                {
                    _viewModel.SelectedCard = card;
                }
                _dragStartPoint = e.GetPosition(null);
                _dragCard = card;
            }
        }

        private void OnSearchResultMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragCard is null)
            {
                return;
            }

            Point current = e.GetPosition(null);
            if (Math.Abs(current.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            CardModel card = _dragCard;
            _dragCard = null;
            DragDrop.DoDragDrop((DependencyObject)sender, card, DragDropEffects.Copy);
        }

        private void OnSearchResultMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (TryGetCard(e.OriginalSource, out CardModel? card))
            {
                if (_viewModel is not null)
                {
                    _viewModel.SelectedCard = card;
                    _viewModel.AddSelectedCardAutomatically();
                }
                e.Handled = true;
            }
        }

        private static bool TryGetCard(object source, out CardModel? card)
        {
            DependencyObject? current = source as DependencyObject;
            while (current is not null)
            {
                if (current is FrameworkElement { DataContext: CardModel dataContext })
                {
                    card = dataContext;
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            card = null;
            return false;
        }

        private void OnSearchResultMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: CardModel card })
            {
                CardHovered?.Invoke(this, card.Id);
            }
        }

        private async void OnSearchResultImageLoaded(object sender, RoutedEventArgs e)
        {
            if (_cardImageCache is null || sender is not Image image ||
                image.Tag is not CardModel card)
            {
                return;
            }

            try
            {
                image.Source = await _cardImageCache.GetCardImageAsync(card.Id);
            }
            catch (HttpRequestException)
            {
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void OnApplyRulesButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.ApplyRules();
        }
    }
}
