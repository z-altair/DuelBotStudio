using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;
using Microsoft.Win32;

namespace DuelBotStudio
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private bool _closeAfterPrompt;
        private bool _closePromptInProgress;

        /// <summary>Creates the main application window and connects its views.</summary>
        public MainWindow()
        {
            InitializeComponent();

            var cardInfoService = new YgoCardInfoService();
            var cardImageCache = new CardImageCache();
            var botProfileService = new BotProfileJsonService();
            _viewModel = new MainWindowViewModel(cardInfoService, new YdkParserService());
            DataContext = _viewModel;

            CardDetailsControl.Initialize(cardInfoService, cardImageCache);
            DeckViewerControl.Initialize(_viewModel.DeckManager, cardImageCache);
            DeckEditorControl.Initialize(cardInfoService, _viewModel.DeckManager);
            BotEditorControl.Initialize(cardInfoService, botProfileService);

            DeckViewerControl.LoadDeckRequested += OnLoadDeckButtonClick;
            DeckViewerControl.SaveDeckRequested += OnSaveDeckButtonClick;
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

            if (!await ConfirmSaveOrDiscardAsync())
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
            catch (InvalidOperationException exception)
            {
                ShowLoadError(exception.Message);
            }
        }

        private async void OnSaveDeckButtonClick(object sender, RoutedEventArgs e)
        {
            await SaveDeckFromDialogAsync();
        }

        private async Task<bool> ConfirmSaveOrDiscardAsync()
        {
            if (!_viewModel.IsDirty)
            {
                return true;
            }

            MessageBoxResult choice = MessageBox.Show(
                "The current deck has unsaved changes. Save them before continuing?",
                "Unsaved deck changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            return choice switch
            {
                MessageBoxResult.Yes => await SaveDeckFromDialogAsync(),
                MessageBoxResult.No => true,
                _ => false
            };
        }

        private async Task<bool> SaveDeckFromDialogAsync()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save deck",
                Filter = "Deck files (*.ydk)|*.ydk",
                DefaultExt = ".ydk",
                AddExtension = true,
                FileName = string.IsNullOrWhiteSpace(_viewModel.DeckManager.CurrentDeck.FilePath)
                    ? "deck.ydk"
                    : Path.GetFileName(_viewModel.DeckManager.CurrentDeck.FilePath)
            };

            if (dialog.ShowDialog() != true)
            {
                return false;
            }

            try
            {
                await _viewModel.SaveDeckAsync(dialog.FileName);
                return true;
            }
            catch (IOException exception)
            {
                ShowSaveError(exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowSaveError(exception.Message);
            }
            catch (ArgumentException exception)
            {
                ShowSaveError(exception.Message);
            }

            return false;
        }

        private async void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (_closeAfterPrompt || (!_viewModel.IsDirty && !BotEditorControl.IsDirty))
            {
                return;
            }

            e.Cancel = true;
            if (_closePromptInProgress)
            {
                return;
            }

            _closePromptInProgress = true;
            try
            {
                if (await ConfirmSaveOrDiscardAsync() && await ConfirmBotSaveOrDiscardAsync())
                {
                    _closeAfterPrompt = true;
                    _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Close));
                }
            }
            finally
            {
                _closePromptInProgress = false;
            }
        }

        private async Task<bool> ConfirmBotSaveOrDiscardAsync()
        {
            if (!BotEditorControl.IsDirty)
            {
                return true;
            }

            MessageBoxResult choice = MessageBox.Show(
                "The current bot profile has unsaved changes. Save them before closing?",
                "Unsaved bot profile changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            return choice switch
            {
                MessageBoxResult.Yes => await BotEditorControl.SaveFromDialogAsync(),
                MessageBoxResult.No => true,
                _ => false
            };
        }

        private static void ShowLoadError(string message)
        {
            MessageBox.Show($"The deck could not be loaded. The current deck was preserved.{Environment.NewLine}{message}",
                "Deck load failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void ShowSaveError(string message)
        {
            MessageBox.Show($"The deck could not be saved.{Environment.NewLine}{message}",
                "Deck save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
