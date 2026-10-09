using System.Collections.Generic;
using System.IO;

namespace DuelBotStudio.Services
{
    public class YdkParserService
    {
        public List<int> ParseFile(string filePath)
        {
            var cardIds = new List<int>();

            foreach (string line in File.ReadLines(filePath))
            {
                string text = line.Trim();

                if (int.TryParse(text, out int id))
                {
                    cardIds.Add(id);
                }
            }

            return cardIds;
        }
    }
}
