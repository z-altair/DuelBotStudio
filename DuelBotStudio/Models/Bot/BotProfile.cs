using DuelBotStudio.Models.Deck;
using System;
using System.Collections.Generic;
using System.Text;

namespace DuelBotStudio.Models.Bot
{
    public class BotProfile
    {
        public string botName { get; set; } = "Custom Bot";
        public DeckModel associatedDeck { get; set; } = new DeckModel();
        public List<BotRule> rules { get; set; } = new List<BotRule>();
    }
}
