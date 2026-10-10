using DuelBotStudio.Services;
using Microsoft.Win32;
using System.Windows;

namespace DuelBotStudio
{
    public partial class MainWindow : Window
    {
        private readonly DeckManager _deckManager;
        private readonly YdkParserService _ydkParserService;

        public MainWindow()
        {
            InitializeComponent();

            var cardInfoService = new YgoCardInfoService();
            _ydkParserService = new YdkParserService();
            _deckManager = new DeckManager(cardInfoService);

            CardDetailsControl.Initialize(cardInfoService);
            DeckViewerControl.Initialize(_deckManager);
            DeckEditorControl.Initialize(cardInfoService, _deckManager);

            DeckViewerControl.LoadDeckRequested += onLoadDeckButtonClick;
            DeckViewerControl.CardSelected += async (_, cardId) => await CardDetailsControl.ShowCardAsync(cardId);
            DeckEditorControl.DeckChanged += async (_, _) => await DeckViewerControl.RefreshAsync();
        }

        private async void onLoadDeckButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
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
            CardDetailsControl.Clear();
            await DeckViewerControl.RefreshAsync();
        }
    }
}
