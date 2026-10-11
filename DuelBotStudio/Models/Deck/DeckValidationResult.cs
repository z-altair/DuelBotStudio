namespace DuelBotStudio.Models.Deck
{
    /// <summary>
    /// Describes whether a deck satisfies its configured structural limits.
    /// </summary>
    public sealed class DeckValidationResult
    {
        /// <summary>Creates a validation result from the supplied messages.</summary>
        public DeckValidationResult(IEnumerable<string> errors)
        {
            Errors = errors.ToArray();
        }

        /// <summary>Gets whether the deck satisfies all configured limits.</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>Gets the validation errors.</summary>
        public IReadOnlyList<string> Errors { get; }
    }
}
