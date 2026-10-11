using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DuelBotStudio.Models.Bot;

namespace DuelBotStudio.Services
{
    /// <summary>Serializes and persists validated bot profile JSON documents.</summary>
    public sealed class BotProfileJsonService
    {
        private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
        private readonly BotProfileValidator _validator;

        /// <summary>Creates the JSON service with the standard schema validator.</summary>
        public BotProfileJsonService(BotProfileValidator? validator = null)
        {
            _validator = validator ?? new BotProfileValidator();
        }

        /// <summary>Serializes a valid profile as indented, versioned JSON.</summary>
        public string Serialize(BotProfile profile)
        {
            IReadOnlyList<string> errors = _validator.Validate(profile);
            if (errors.Count > 0)
            {
                throw new InvalidDataException(string.Join(Environment.NewLine, errors));
            }

            return JsonSerializer.Serialize(profile, JsonOptions);
        }

        /// <summary>Deserializes and validates a bot profile JSON document.</summary>
        public BotProfile Deserialize(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            BotProfile profile = JsonSerializer.Deserialize<BotProfile>(json, JsonOptions)
                ?? throw new InvalidDataException("The bot profile JSON document is empty.");
            IReadOnlyList<string> errors = _validator.Validate(profile);
            if (errors.Count > 0)
            {
                throw new InvalidDataException(string.Join(Environment.NewLine, errors));
            }

            return profile;
        }

        /// <summary>Reads, deserializes, and validates a bot profile file.</summary>
        public async Task<BotProfile> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            string json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            return Deserialize(json);
        }

        /// <summary>Validates and writes a bot profile file.</summary>
        public async Task WriteFileAsync(
            BotProfile profile,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            string json = Serialize(profile);
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
                await File.WriteAllTextAsync(
                    temporaryPath,
                    json,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    cancellationToken).ConfigureAwait(false);
                File.Move(temporaryPath, fullPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
            return options;
        }
    }
}
