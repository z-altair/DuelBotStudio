using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DuelBotStudio.Models.Bot
{
    /// <summary>A named strategy mode used to scope rules and represent state transitions.</summary>
    public sealed class BotMode : INotifyPropertyChanged
    {
        private string _name = string.Empty;

        /// <summary>Gets or sets the stable mode identifier.</summary>
        [JsonRequired]
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the display name.</summary>
        [JsonRequired]
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                {
                    return;
                }

                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
