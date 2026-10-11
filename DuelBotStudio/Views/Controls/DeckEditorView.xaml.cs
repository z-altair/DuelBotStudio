using System.Windows;
using System.Windows.Controls;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;

namespace DuelBotStudio.Views.Controls
{
    /// <summary>
    /// Presents card search, explicit deck-section selection, and validation feedback.
    /// </summary>
    public partial class DeckEditorView : UserControl
    {
        private DeckEditorViewModel? _viewModel;

        /// <summary>Creates the deck editor view.</summary>
        public DeckEditorView()
        {
            InitializeComponent();
        }

        /// <summary>Connects the view to its services and active deck manager.</summary>
        public void Initialize(YgoCardInfoService cardInfoService, DeckManager deckManager)
        {
            _viewModel = new DeckEditorViewModel(cardInfoService, deckManager);
            DataContext = _viewModel;
        }

        private async void OnSearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel is not null)
            {
                await _viewModel.SearchAsync();
            }
        }

        private void OnAddCardButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.AddSelectedCard();
        }

        private void OnApplyRulesButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.ApplyRules();
        }
    }
}
