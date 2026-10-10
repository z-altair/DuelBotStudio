using DuelBotStudio.Models.Card;
using DuelBotStudio.Services;
using System.Windows;
using System.Windows.Controls;

namespace DuelBotStudio.Views.Controls
{
    public partial class DeckEditorView : UserControl
    {
        private YgoCardInfoService? _cardInfoService;
        private DeckManager? _deckManager;

        public event EventHandler? DeckChanged;

        public DeckEditorView()
        {
            InitializeComponent();
        }

        public void Initialize(YgoCardInfoService cardInfoService, DeckManager deckManager)
        {
            _cardInfoService = cardInfoService;
            _deckManager = deckManager;
        }

        private async void onSearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (_cardInfoService == null)
            {
                return;
            }

            string query = SearchInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                SearchResultsListBox.ItemsSource = null;
                return;
            }

            SearchResultsListBox.ItemsSource = await _cardInfoService.searchCardsByNameAsync(query);
        }

        private void onSearchResultRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_deckManager == null ||
                (e.OriginalSource as FrameworkElement)?.DataContext is not CardModel selectedCard)
            {
                return;
            }

            _deckManager.addCard(selectedCard);
            DeckChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }
}
