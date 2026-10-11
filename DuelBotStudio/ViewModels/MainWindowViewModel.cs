using DuelBotStudio.Models;
using DuelBotStudio.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DuelBotStudio.ViewModels
{
    /// <summary>Coordinates deck import and exposes the shared deck manager to the views.</summary>
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly YdkParserService _parser;
        private readonly YdkWriterService _writer;
        private bool _isDirty;
        private int _deckChangeVersion;

        /// <summary>Creates the workspace view model.</summary>
        /// <param name="cardInfoService">Service used to look up imported card metadata.</param>
        /// <param name="parser">Parser used to validate YDK files.</param>
        /// <param name="writer">Writer used to export YDK files.</param>
        public MainWindowViewModel(
            YgoCardInfoService cardInfoService,
            YdkParserService parser,
            YdkWriterService? writer = null)
        {
            DeckManager = new DeckManager(cardInfoService);
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _writer = writer ?? new YdkWriterService();
            DeckManager.DeckChanged += OnDeckChanged;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Gets the manager for the active deck.</summary>
        public DeckManager DeckManager { get; }

        /// <summary>Gets whether the active deck has changes not yet saved.</summary>
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty == value)
                {
                    return;
                }

                _isDirty = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        /// <summary>Gets the application title, marked when the active deck has unsaved changes.</summary>
        public string WindowTitle => IsDirty ? "DuelBot Studio *" : "DuelBot Studio";

        /// <summary>
        /// Parses and loads a deck. Parser errors are returned without changing the active deck;
        /// retrieval or persistence errors propagate to the caller.
        /// </summary>
        /// <param name="filePath">Path to the YDK file.</param>
        /// <returns>Parser errors, or an empty list after a successful load.</returns>
        public async Task<IReadOnlyList<string>> LoadDeckAsync(string filePath)
        {
            YdkParseResult result = _parser.ParseFile(filePath);
            if (result.HasErrors)
            {
                return result.Errors;
            }

            await DeckManager.LoadFromParseResultAsync(result, filePath);
            IsDirty = false;
            return Array.Empty<string>();
        }

        /// <summary>Exports the active deck as a YDK file and clears its dirty state.</summary>
        /// <param name="filePath">Destination path for the deck file.</param>
        public async Task SaveDeckAsync(string filePath)
        {
            int saveVersion = _deckChangeVersion;
            await _writer.WriteFileAsync(DeckManager.CurrentDeck, filePath);
            if (saveVersion == _deckChangeVersion)
            {
                DeckManager.MarkSaved(filePath);
                IsDirty = false;
            }
        }

        private void OnDeckChanged(object? sender, EventArgs e)
        {
            _deckChangeVersion++;
            IsDirty = true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
