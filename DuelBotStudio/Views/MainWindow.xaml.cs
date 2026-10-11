using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;
using Microsoft.Win32;

namespace DuelBotStudio
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        /// <summary>Creates the main application window and connects its views.</summary>
        public MainWindow()
        {
            InitializeComponent();

            var cardInfoService = new YgoCardInfoService();
            var cardImageCache = new CardImageCache();
            _viewModel = new MainWindowViewModel(cardInfoService, new YdkParserService());

            CardDetailsControl.Initialize(cardInfoService, cardImageCache);
            DeckViewerControl.Initialize(_viewModel.DeckManager, cardImageCache);
            DeckEditorControl.Initialize(cardInfoService, _viewModel.DeckManager);

            DeckViewerControl.LoadDeckRequested += OnLoadDeckButtonClick;
            DeckViewerControl.CardSelected += async (_, cardId) => await CardDetailsControl.ShowCardAsync(cardId);
        }

        private async void OnLoadDeckButtonClick(object sender, RoutedEventArgs e)
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

            try
            {
                var errors = await _viewModel.LoadDeckAsync(dialog.FileName);
                if (errors.Count > 0)
                {
                    MessageBox.Show(string.Join(Environment.NewLine, errors), "Invalid deck file", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CardDetailsControl.Clear();
            }
            catch (HttpRequestException exception)
            {
                ShowLoadError(exception.Message);
            }
            catch (IOException exception)
            {
                ShowLoadError(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowLoadError(exception.Message);
            }
            catch (JsonException exception)
            {
                ShowLoadError(exception.Message);
            }
        }

        private static void ShowLoadError(string message)
        {
            MessageBox.Show($"The deck could not be loaded. The current deck was preserved.{Environment.NewLine}{message}",
                "Deck load failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
