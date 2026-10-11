namespace DuelBotStudio.Models
{
    /// <summary>
    /// Contains card identifiers and diagnostics produced while parsing a YDK file.
    /// </summary>
    public class YdkParseResult
    {
        /// <summary>Gets the identifiers assigned to the main deck.</summary>
        public List<int> MainDeckIds { get; } = new();

        /// <summary>Gets the identifiers assigned to the extra deck.</summary>
        public List<int> ExtraDeckIds { get; } = new();

        /// <summary>Gets the identifiers assigned to the side deck.</summary>
        public List<int> SideDeckIds { get; } = new();

        /// <summary>Gets the source line numbers for each parsed card identifier.</summary>
        public Dictionary<int, List<int>> CardIdLineNumbers { get; } = new();

        /// <summary>Gets whether parsing found one or more errors.</summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>Gets line-numbered parsing or file-access errors.</summary>
        public List<string> Errors { get; } = new();
    }
}
