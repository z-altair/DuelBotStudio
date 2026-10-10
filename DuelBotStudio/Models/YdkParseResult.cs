using System.Collections.Generic;

namespace DuelBotStudio.Models
{
    public class YdkParseResult
    {
        public List<int> mainDeckIds { get; set; } = new List<int>();
        public List<int> extraDeckIds { get; set; } = new List<int>();
        public List<int> sideDeckIds { get; set; } = new List<int>();

        public bool hasErrors => errors.Count > 0;
        public List<string> errors { get; set; } = new List<string>();
    }
}
