namespace DuelBotStudio.Models.Card
{
    public class CardModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
        public string Race { get; set; } = string.Empty;
        public int? Atk { get; set; }
        public int? Def { get; set; }
        public int? Level { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
