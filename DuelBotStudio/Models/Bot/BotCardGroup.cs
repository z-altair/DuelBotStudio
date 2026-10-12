using System.Text.Json.Serialization;

namespace DuelBotStudio.Models.Bot
{
    /// <summary>A user-defined reusable set of cards such as starters or negations.</summary>
    public sealed class BotCardGroup
    {
        /// <summary>Gets or sets the stable group identifier.</summary>
        [JsonRequired]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>Gets or sets the user-facing group name.</summary>
        [JsonRequired]
        public string Name { get; set; } = "New group";

        /// <summary>Gets or sets an optional description of the group's purpose.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the cards currently assigned to the group.</summary>
        public List<int> CardIds { get; set; } = new();

        /// <summary>Gets or sets the priority used when choosing within the group.</summary>
        public int Priority { get; set; }
    }
}
