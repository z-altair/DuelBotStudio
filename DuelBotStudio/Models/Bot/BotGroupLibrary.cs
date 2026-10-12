using System.Text.Json.Serialization;

namespace DuelBotStudio.Models.Bot
{
    /// <summary>Reusable card groups shared by bot profiles.</summary>
    public sealed class BotGroupLibrary
    {
        /// <summary>The current group library document version.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Gets or sets the document schema version.</summary>
        [JsonRequired]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>Gets or sets reusable card groups.</summary>
        [JsonRequired]
        public List<BotCardGroup> Groups { get; set; } = new();
    }
}
