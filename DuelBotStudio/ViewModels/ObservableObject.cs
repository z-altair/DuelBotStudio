using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DuelBotStudio.ViewModels
{
    /// <summary>Provides property-change notification and a shared property setter for view models.</summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Updates a field and raises a property-change notification when its value changes.</summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>Raises a property-change notification.</summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
