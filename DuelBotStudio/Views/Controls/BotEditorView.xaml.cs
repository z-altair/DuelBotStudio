using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DuelBotStudio.Services;
using DuelBotStudio.ViewModels;
using Microsoft.Win32;

namespace DuelBotStudio.Views.Controls
{
    /// <summary>Presents a structured editor for bot rules and strategy modes.</summary>
    public partial class BotEditorView : UserControl
    {
        private BotEditorViewModel? _viewModel;

        /// <summary>Creates the bot editor view.</summary>
        public BotEditorView()
        {
            InitializeComponent();
        }

        /// <summary>Connects card lookup and profile persistence services to the editor.</summary>
        public void Initialize(YgoCardInfoService cardInfoService, BotProfileJsonService profileService)
        {
            _viewModel = new BotEditorViewModel(cardInfoService, profileService);
            DataContext = _viewModel;
        }

        /// <summary>Gets whether the active bot profile has unsaved changes.</summary>
        public bool IsDirty => _viewModel?.IsDirty == true;

        /// <summary>Prompts for a JSON destination and saves the active profile.</summary>
        public async Task<bool> SaveFromDialogAsync()
        {
            if (_viewModel is null)
            {
                return false;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Save bot profile",
                Filter = "Bot profiles (*.json)|*.json",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = string.IsNullOrWhiteSpace(_viewModel.FilePath)
                    ? "bot-profile.json"
                    : Path.GetFileName(_viewModel.FilePath)
            };

            if (dialog.ShowDialog() != true)
            {
                return false;
            }

            try
            {
                await _viewModel.SaveProfileAsync(dialog.FileName);
                return true;
            }
            catch (Exception exception) when (
                exception is IOException or
                UnauthorizedAccessException or
                InvalidDataException or
                JsonException or
                ArgumentException)
            {
                _viewModel.ReportFileError(exception.Message);
                return false;
            }
        }

        private async void OnSearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel is not null)
            {
                await _viewModel.SearchCardsAsync();
            }
        }

        private async void OnSaveButtonClick(object sender, RoutedEventArgs e) =>
            await SaveFromDialogAsync();

        private async void OnLoadButtonClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel is null || !await ConfirmDiscardChangesAsync())
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Open bot profile",
                Filter = "Bot profiles (*.json)|*.json"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                await _viewModel.LoadProfileAsync(dialog.FileName);
            }
            catch (Exception exception) when (
                exception is IOException or
                UnauthorizedAccessException or
                InvalidDataException or
                JsonException or
                ArgumentException)
            {
                _viewModel.ReportFileError(exception.Message);
            }
        }

        private async Task<bool> ConfirmDiscardChangesAsync()
        {
            if (_viewModel?.IsDirty != true)
            {
                return true;
            }

            MessageBoxResult choice = MessageBox.Show(
                "The current bot profile has unsaved changes. Save them before loading another profile?",
                "Unsaved bot profile changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            return choice switch
            {
                MessageBoxResult.Yes => await SaveFromDialogAsync(),
                MessageBoxResult.No => true,
                _ => false
            };
        }

        private void OnNewRuleButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.BeginNewRule();

        private void OnApplyRuleButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.ApplyRule();

        private void OnDeleteRuleButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.RemoveSelectedRule();

        private void OnAddConditionButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.Draft.AddCondition();

        private void OnRemoveConditionButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.Draft.RemoveSelectedCondition();

        private void OnAddModeButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.AddMode();

        private void OnRemoveModeButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.RemoveSelectedMode();

        private void OnAddGroupButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.AddGroup();

        private void OnRemoveGroupButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.RemoveSelectedGroup();

        private void OnAddSelectedCardToGroupButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.AddSelectedCardToGroup();

        private void OnUseCardAsTargetButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.UseSelectedCardAsTarget();

        private void OnRegisterCardButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.RegisterSelectedCard();

        private void OnUseSelectedCardAsSubjectButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.UseSelectedCardAsSubject();

        private void OnUseCardInConditionButtonClick(object sender, RoutedEventArgs e) =>
            _viewModel?.UseSelectedCardInCondition();
    }
}
