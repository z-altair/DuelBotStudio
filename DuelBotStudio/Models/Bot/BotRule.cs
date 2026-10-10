using System;
using System.Collections.Generic;
using System.Text;

namespace DuelBotStudio.Models.Bot
{
    public class BotRule
    {
        public int CardId { get; set; }
        public string CardName { get; set; } = string.Empty;
        public BotActionType Action { get; set; }
        public string ConditionExpression { get; set; } = string.Empty;
        public int Priority { get; set; }

    }
}
