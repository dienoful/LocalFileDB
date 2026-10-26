using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LocalFileDB.ViewModels
{
    /// <summary>
    /// Represents a tag option and notifies the catalog when its selection changes.
    /// </summary>
    public sealed class TagFilterItemViewModel : INotifyPropertyChanged
    {
        private readonly Action _selectionChanged;
        private bool _isSelected;

        /// <summary>
        /// Creates a tag filter option with its current selection state.
        /// </summary>
        public TagFilterItemViewModel(string name, bool isSelected, Action selectionChanged)
        {
            Name = name;
            _isSelected = isSelected;
            _selectionChanged = selectionChanged;
        }

        /// <summary>
        /// Raised when a bindable property changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the tag displayed in the filter menu.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets whether this tag participates in filtering.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                _selectionChanged();
            }
        }
    }
}
