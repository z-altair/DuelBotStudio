using DuelBotStudio.Models;
using DuelBotStudio.Models.Deck;
using System.Globalization;
using System.IO;

namespace DuelBotStudio.Services
{
    /// <summary>
    /// Parses YDK deck files and reports malformed lines without throwing away diagnostics.
    /// </summary>
    public class YdkParserService
    {
        /// <summary>
        /// Reads and validates a YDK file. Supported directives are <c>#main</c>,
        /// <c>#extra</c>, <c>!side</c>, and the conventional <c>#created by</c> metadata.
        /// </summary>
        /// <param name="filePath">Path to the YDK file.</param>
        /// <returns>Parsed card identifiers and any line-numbered errors.</returns>
        public YdkParseResult ParseFile(string filePath)
        {
            var result = new YdkParseResult();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.Errors.Add("The file path is empty.");
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.Errors.Add($"File does not exist: {filePath}");
                return result;
            }

            DeckSection? currentSection = null;
            var lineNumber = 0;

            try
            {
                foreach (string line in File.ReadLines(filePath))
                {
                    lineNumber++;
                    string text = line.Trim();

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    if (text.Equals("#main", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = DeckSection.Main;
                        continue;
                    }

                    if (text.Equals("#extra", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = DeckSection.Extra;
                        continue;
                    }

                    if (text.Equals("!side", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = DeckSection.Side;
                        continue;
                    }

                    if (text.StartsWith("#created by", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (text.StartsWith('#') || text.StartsWith('!'))
                    {
                        result.Errors.Add($"Line {lineNumber}: Unrecognized section directive '{text}'.");
                        continue;
                    }

                    if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int cardId) || cardId <= 0)
                    {
                        result.Errors.Add($"Line {lineNumber}: Expected a positive numeric card ID, but found '{text}'.");
                        continue;
                    }

                    if (currentSection is null)
                    {
                        result.Errors.Add($"Line {lineNumber}: Card ID {cardId} appears before a deck section.");
                        continue;
                    }

                    switch (currentSection.Value)
                    {
                        case DeckSection.Main:
                            result.MainDeckIds.Add(cardId);
                            break;
                        case DeckSection.Extra:
                            result.ExtraDeckIds.Add(cardId);
                            break;
                        case DeckSection.Side:
                            result.SideDeckIds.Add(cardId);
                            break;
                    }

                    if (!result.CardIdLineNumbers.TryGetValue(cardId, out var sourceLines))
                    {
                        sourceLines = new List<int>();
                        result.CardIdLineNumbers.Add(cardId, sourceLines);
                    }

                    sourceLines.Add(lineNumber);
                }
            }
            catch (IOException exception)
            {
                result.Errors.Add($"Unable to read '{filePath}': {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                result.Errors.Add($"Unable to read '{filePath}': {exception.Message}");
            }
            catch (System.Security.SecurityException exception)
            {
                result.Errors.Add($"Unable to read '{filePath}': {exception.Message}");
            }

            if (currentSection is null && result.Errors.Count == 0)
            {
                result.Errors.Add("The file does not contain a deck section.");
            }

            return result;
        }
    }
}
