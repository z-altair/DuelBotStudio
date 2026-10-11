using System.IO;
using System.Text;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Models.Deck;

namespace DuelBotStudio.Services
{
    /// <summary>Writes deck sections to the conventional YDK text format.</summary>
    public sealed class YdkWriterService
    {
        /// <summary>Writes a deck to a YDK file, replacing the destination only after writing completes.</summary>
        /// <param name="deck">Deck to export.</param>
        /// <param name="filePath">Destination path.</param>
        /// <exception cref="InvalidDataException">A card has a non-positive ID.</exception>
        public async Task WriteFileAsync(DeckModel deck, string filePath)
        {
            ArgumentNullException.ThrowIfNull(deck);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var contents = new StringBuilder();
            contents.AppendLine("#created by DuelBot Studio");
            AppendSection(contents, "#main", deck.MainDeck);
            AppendSection(contents, "#extra", deck.ExtraDeck);
            AppendSection(contents, "!side", deck.SideDeck);

            string fullPath = Path.GetFullPath(filePath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new IOException($"Unable to determine the destination directory for '{filePath}'.");
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllTextAsync(temporaryPath, contents.ToString(), new UTF8Encoding(false));
                File.Move(temporaryPath, fullPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static void AppendSection(StringBuilder contents, string directive, IReadOnlyList<CardModel> cards)
        {
            contents.AppendLine(directive);
            foreach (CardModel card in cards)
            {
                if (card.Id <= 0)
                {
                    throw new InvalidDataException($"Card '{card.Name}' has an invalid ID: {card.Id}.");
                }

                contents.AppendLine(card.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }
}
