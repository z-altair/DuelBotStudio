namespace DuelBotStudio.Models.Card
{
    public class CardModel
    {
        public int id { get; set; }                 // Passcode/Konami ID 
        public string name { get; set; } = string.Empty;            // Card name
        public string description { get; set; } = string.Empty;     // Card effect
        public string type { get; set; } = string.Empty;            // Normal Monster, Spell Card, Trap Card, Fusion Monster, etc.
        public string attribute { get; set; } = string.Empty;       // DARK, LIGHT, WATER, etc.
        public string race { get; set; } = string.Empty;            // Spellcaster, Dragon, Continuous, etc.
        public int? atk { get; set; }
        public int? def { get; set; }
        public int? level { get; set; }             // level / rank / link rating
        public string imageUrl { get; set; } = string.Empty;
    }
}
