using System;
using System.Collections.Generic;
using System.Text;

namespace DuelBotStudio.Models.Bot
{
    public class BotRule
    {
        public int cardId {  get; set; }
        public string cardName { get; set; } = string.Empty;
        public BotActionType action {  get; set; }
        public string conditionExpression { get; set; } = string.Empty;
        public int priority { get; set; }

    }
}
