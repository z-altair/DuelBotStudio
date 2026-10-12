using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;
using Microsoft.Win32;

namespace DuelBotStudio.Views.Controls
{
    /// <summary>Provides a standalone editor for reusable card groups.</summary>
    public partial class BotGroupEditorView : UserControl
    {
        private BotGroupEditorViewModel? _viewModel;

        /// <summary>Creates the group editor.</summary>
        public BotGroupEditorView()
        {
            InitializeComponent();
        }

        /// <summary>Initializes the group editor services.</summary>
        public void Initialize(YgoCardInfoService cardInfoService, BotGroupLibraryJsonService libraryService)
        {
            _viewModel = new BotGroupEditorViewModel(cardInfoService, libraryService);
            DataContext = _viewModel;
        }

        private void OnNewGroupClick(object sender, RoutedEventArgs e) => _viewModel?.CreateGroup();

        private void OnRemoveGroupClick(object sender, RoutedEventArgs e) => _viewModel?.RemoveSelectedGroup();

        private async void OnSearchClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel is not null)
            {
                await _viewModel.SearchCardsAsync();
            }
        }

        private async void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel is not null)
            {
                await _viewModel.SearchCardsAsync();
                e.Handled = true;
            }
        }

        private void OnAddCardClick(object sender, RoutedEventArgs e) => _viewModel?.AddSelectedCard();

        private void OnRemoveCardClick(object sender, RoutedEventArgs e) => _viewModel?.RemoveSelectedCard();

        private void OnGroupDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.SelectedGroup is not null)
            {
                e.Handled = true;
            }
        }

        private async void OnOpenClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel is null)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Group libraries (*.groups.json)|*.groups.json|JSON files (*.json)|*.json"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _viewModel.LoadAsync(dialog.FileName);
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    _viewModel.ReportError(exception.Message);
                }
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel is null)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Group libraries (*.groups.json)|*.groups.json|JSON files (*.json)|*.json",
                DefaultExt = ".groups.json",
                AddExtension = true,
                FileName = "card-groups.groups.json"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _viewModel.SaveAsync(dialog.FileName);
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    _viewModel.ReportError(exception.Message);
                }
            }
        }
    }
}
