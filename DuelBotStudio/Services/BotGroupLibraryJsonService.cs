using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DuelBotStudio.Models.Bot;

namespace DuelBotStudio.Services
{
    /// <summary>Persists reusable card groups independently from bot profiles.</summary>
    public sealed class BotGroupLibraryJsonService
    {
        private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

        /// <summary>Serializes a valid group library.</summary>
        public string Serialize(BotGroupLibrary library)
        {
            Validate(library);
            return JsonSerializer.Serialize(library, JsonOptions);
        }

        /// <summary>Deserializes and validates a group library.</summary>
        public BotGroupLibrary Deserialize(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            BotGroupLibrary library = JsonSerializer.Deserialize<BotGroupLibrary>(json, JsonOptions)
                ?? throw new InvalidDataException("The group library is empty.");
            Validate(library);
            return library;
        }

        /// <summary>Reads a group library from disk.</summary>
        public async Task<BotGroupLibrary> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            string json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            return Deserialize(json);
        }

        /// <summary>Validates and writes a group library to disk.</summary>
        public async Task WriteFileAsync(
            BotGroupLibrary library,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            string fullPath = Path.GetFullPath(filePath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new IOException($"Unable to determine the destination directory for '{filePath}'.");
            }

            string json = Serialize(library);
            Directory.CreateDirectory(directory);
            string temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllTextAsync(
                    temporaryPath,
                    json,
                    new UTF8Encoding(false),
                    cancellationToken).ConfigureAwait(false);
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

        private static void Validate(BotGroupLibrary library)
        {
            ArgumentNullException.ThrowIfNull(library);
            if (library.SchemaVersion != BotGroupLibrary.CurrentSchemaVersion)
            {
                throw new InvalidDataException($"Unsupported group library schema version: {library.SchemaVersion}.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (BotCardGroup? group in library.Groups ?? throw new InvalidDataException("Groups cannot be null."))
            {
                if (group is null || string.IsNullOrWhiteSpace(group.Id))
                {
                    throw new InvalidDataException("Every group must have an ID.");
                }

                if (!ids.Add(group.Id))
                {
                    throw new InvalidDataException($"Duplicate group ID: {group.Id}.");
                }

                if (string.IsNullOrWhiteSpace(group.Name))
                {
                    throw new InvalidDataException($"Group '{group.Id}' must have a name.");
                }

                if (group.CardIds is null || group.CardIds.Any(id => id <= 0))
                {
                    throw new InvalidDataException($"Group '{group.Name}' contains an invalid card ID.");
                }

                if (group.RepresentativeCardId.HasValue &&
                    !group.CardIds.Contains(group.RepresentativeCardId.Value))
                {
                    throw new InvalidDataException(
                        $"Group '{group.Name}' has a representative card that is not in the group.");
                }
            }
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                WriteIndented = true
            };
        }
    }
}
