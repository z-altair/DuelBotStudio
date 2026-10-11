using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DuelBotStudio.Models.Bot
{
    /// <summary>A typed predicate used to decide whether a rule is eligible.</summary>
    public sealed class BotCondition : INotifyPropertyChanged
    {
        private BotConditionType _type;
        private int? _cardId;
        private string _cardName = string.Empty;
        private BotCardZone? _zone;
        private int? _minimumCount;

        /// <summary>Gets or sets the predicate type.</summary>
        [JsonRequired]
        public BotConditionType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>Gets or sets the card ID used by card-specific predicates.</summary>
        public int? CardId
        {
            get => _cardId;
            set => SetProperty(ref _cardId, value);
        }

        /// <summary>Gets or sets the card name for display.</summary>
        public string CardName
        {
            get => _cardName;
            set => SetProperty(ref _cardName, value);
        }

        /// <summary>Gets or sets the zone used by the card-presence predicate.</summary>
        public BotCardZone? Zone
        {
            get => _zone;
            set => SetProperty(ref _zone, value);
        }

        /// <summary>Gets or sets the minimum use count for the effect-use predicate.</summary>
        public int? MinimumCount
        {
            get => _minimumCount;
            set => SetProperty(ref _minimumCount, value);
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
