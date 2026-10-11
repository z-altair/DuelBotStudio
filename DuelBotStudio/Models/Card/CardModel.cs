namespace DuelBotStudio.Models.Card
{
    /// <summary>Represents card data used by deck tools, card details, and bot rule references.</summary>
    public class CardModel
    {
        /// <summary>Gets or sets the card database identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the card name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the card effect or description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the card type.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Gets or sets the card attribute.</summary>
        public string Attribute { get; set; } = string.Empty;

        /// <summary>Gets or sets the card race.</summary>
        public string Race { get; set; } = string.Empty;

        /// <summary>Gets or sets the attack value, when available.</summary>
        public int? Atk { get; set; }

        /// <summary>Gets or sets the defense value, when available.</summary>
        public int? Def { get; set; }

        /// <summary>Gets or sets the level or rank, when available.</summary>
        public int? Level { get; set; }

        /// <summary>Gets or sets the card image URL.</summary>
        public string ImageUrl { get; set; } = string.Empty;
    }
}
