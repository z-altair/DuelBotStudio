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
                result.errors.Add($"File doesn't exist: {filePath}");
                return result;
            }

            DeckSection currentSection = DeckSection.MAIN;

            foreach (string line in File.ReadLines(filePath))
            {
                string text = line.Trim();

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if(text.Equals("#main", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = DeckSection.MAIN;
                    continue;
                }
                if (text.Equals("#extra", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = DeckSection.EXTRA;
                    continue;
                }
                if (text.Equals("!side", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = DeckSection.SIDE;
                    continue;
                }

                if (int.TryParse(text, out int cardId))
                {
                    switch (currentSection)
                    {
                        case DeckSection.MAIN:
                            result.mainDeckIds.Add(cardId);
                            break;
                        case DeckSection.EXTRA:
                            result.extraDeckIds.Add(cardId);
                            break;
                        case DeckSection.SIDE:
                            result.sideDeckIds.Add(cardId);
                            break;
                    }
                }
            }

            return result;
        }
    }
}
