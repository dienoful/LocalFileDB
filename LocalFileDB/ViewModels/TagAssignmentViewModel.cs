using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LocalFileDB.ViewModels
{
    /// <summary>
    /// Represents one existing tag that can be assigned to a file.
    /// </summary>
    public sealed class TagAssignmentViewModel : INotifyPropertyChanged
    {
        private readonly Action _assignmentChanged;
        private bool _isAssigned;

        /// <summary>
        /// Creates an assignment option for a tag.
        /// </summary>
        public TagAssignmentViewModel(string name, bool isAssigned, Action assignmentChanged)
        {
            Name = name;
            _isAssigned = isAssigned;
            _assignmentChanged = assignmentChanged;
        }

        /// <summary>
        /// Raised when a bindable property changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the tag name displayed beside the checkbox.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets whether the tag is assigned to its file.
        /// </summary>
        public bool IsAssigned
        {
            get => _isAssigned;
            set
            {
                if (_isAssigned == value)
                {
                    return;
                }

                _isAssigned = value;
                OnPropertyChanged();
                _assignmentChanged();
            }
        }

        /// <summary>
        /// Raises the property-changed event for WPF bindings.
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
