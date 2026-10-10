using DuelBotStudio.Models.Deck;
using System;
using System.Collections.Generic;
using System.Text;

namespace DuelBotStudio.Models.Bot
{
    public class BotProfile
    {
        public string BotName { get; set; } = "Custom Bot";
        public DeckModel AssociatedDeck { get; set; } = new DeckModel();
        public List<BotRule> Rules { get; set; } = new List<BotRule>();
    }
}
