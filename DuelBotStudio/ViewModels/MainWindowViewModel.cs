using DuelBotStudio.Models;
using DuelBotStudio.Services;

namespace DuelBotStudio.ViewModels
{
    /// <summary>Coordinates deck import and exposes the shared deck manager to the views.</summary>
    public sealed class MainWindowViewModel
    {
        private readonly YdkParserService _parser;

        /// <summary>Creates the workspace view model.</summary>
        /// <param name="cardInfoService">Service used to look up imported card metadata.</param>
        /// <param name="parser">Parser used to validate YDK files.</param>
        public MainWindowViewModel(YgoCardInfoService cardInfoService, YdkParserService parser)
        {
            DeckManager = new DeckManager(cardInfoService);
            _parser = parser;
        }

        /// <summary>Gets the manager for the active deck.</summary>
        public DeckManager DeckManager { get; }

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

            await DeckManager.LoadFromParseResultAsync(result);
            return Array.Empty<string>();
        }
    }
}
