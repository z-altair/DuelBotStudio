using System.Text.Json.Serialization;

namespace DuelBotStudio.Models.Bot
{
    /// <summary>
    /// Serializable bot behavior profile, independent of deck data and runtime implementation.
    /// </summary>
    public class BotProfile
    {
        /// <summary>The current version of the bot profile document schema.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Gets or sets the profile document schema version.</summary>
        [JsonRequired]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>Gets or sets the profile name.</summary>
        [JsonRequired]
        public string BotName { get; set; } = "Custom Bot";

        /// <summary>Gets or sets the identifier of the mode used when a duel starts.</summary>
        [JsonRequired]
        public string InitialModeId { get; set; } = "combo";

        /// <summary>Gets or sets the named modes that rules can use and transition between.</summary>
        [JsonRequired]
        public List<BotMode> Modes { get; set; } =
        [
            new BotMode { Id = "combo", Name = "Combo" },
            new BotMode { Id = "defensive", Name = "Defensive" }
        ];

        /// <summary>Gets or sets persistent card behavior metadata, including cards absent from the deck.</summary>
        public List<BotCardProfile> Cards { get; set; } = new();

        /// <summary>Gets or sets reusable user-defined card groups.</summary>
        public List<BotCardGroup> Groups { get; set; } = new();

        /// <summary>Gets or sets the ordered decision rules in this profile.</summary>
        [JsonRequired]
        public List<BotRule> Rules { get; set; } = new();
    }
}
