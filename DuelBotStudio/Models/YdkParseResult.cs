using System.Collections.Generic;

namespace DuelBotStudio.Models
{
    public class YdkParseResult
    {
        public List<int> MainDeckIds { get; set; } = new List<int>();
        public List<int> ExtraDeckIds { get; set; } = new List<int>();
        public List<int> SideDeckIds { get; set; } = new List<int>();

        public bool HasErrors => Errors.Count > 0;
        public List<string> Errors { get; set; } = new List<string>();
    }
}
