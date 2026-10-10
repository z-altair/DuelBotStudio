using DuelBotStudio.Models;
using DuelBotStudio.Models.Deck;
using System.Collections.Generic;
using System.IO;

namespace DuelBotStudio.Services
{
    public class YdkParserService
    {
        public YdkParseResult ParseFile(string filePath)
        {
            var result = new YdkParseResult();

            if (!File.Exists(filePath))
            {
                result.Errors.Add($"File doesn't exist: {filePath}");
                return result;
            }

            DeckSection currentSection = DeckSection.Main;

            foreach (string line in File.ReadLines(filePath))
            {
                string text = line.Trim();

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if(text.Equals("#main", StringComparison.OrdinalIgnoreCase))
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

                if (int.TryParse(text, out int cardId))
                {
                    switch (currentSection)
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
                }
            }

            return result;
        }
    }
}
