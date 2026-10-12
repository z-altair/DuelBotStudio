using System.Text.Json.Serialization;

namespace DuelBotStudio.Models.Bot
{
    /// <summary>Persistent behavior metadata for a card, independent of the current deck.</summary>
    public sealed class BotCardProfile
    {
        /// <summary>Gets or sets the stable card database identifier.</summary>
        [JsonRequired]
        public int CardId { get; set; }

        /// <summary>Gets or sets the card name cached for display.</summary>
        public string CardName { get; set; } = string.Empty;

        /// <summary>Gets or sets whether rules for this card are currently enabled.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Gets or sets free-form notes describing how the card is used.</summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>Gets or sets the IDs of groups containing this card.</summary>
        public List<string> GroupIds { get; set; } = new();
    }
}
