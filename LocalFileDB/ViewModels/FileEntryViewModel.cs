using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LocalFileDB.Commands;
using LocalFileDB.Models;
using LocalFileDB.Services;

namespace LocalFileDB.ViewModels
{
    /// <summary>
    /// Exposes one catalog entry in a form suitable for the file list and preview panel.
    /// </summary>
    public sealed class FileEntryViewModel : INotifyPropertyChanged
    {
        private readonly Action _tagsChanged;

        /// <summary>
        /// Creates a display item and connects its tag and row commands to the catalog owner.
        /// </summary>
        public FileEntryViewModel(
            FileDatabaseEntry entry,
            string rootDirectory,
            IEnumerable<string> availableTags,
            Action<FileEntryViewModel> remove,
            Action tagsChanged)
        {
            Entry = entry;
            Entry.Tags ??= [];
            ResolvedPath = FileDatabaseService.ResolvePath(entry, rootDirectory);
            FileName = System.IO.Path.GetFileName(ResolvedPath);
            MediaType = entry.MediaType;
            RemoveCommand = new RelayCommand(_ => remove(this));
            _tagsChanged = tagsChanged;

            foreach (var tag in availableTags.Concat(Entry.Tags).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                AddAvailableTag(tag);
            }
        }

        /// <summary>
        /// Raised when a bindable property changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the persisted data represented by this list item.
        /// </summary>
        public FileDatabaseEntry Entry { get; }

        /// <summary>
        /// Gets the resolved path shown in the table and preview panel.
        /// </summary>
        public string ResolvedPath { get; }

        /// <summary>
        /// Gets the file name shown at the top of the preview panel.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the image or video classification stored in the catalog.
        /// </summary>
        public string MediaType { get; }

        /// <summary>
        /// Gets the comma-separated tags assigned to the file.
        /// </summary>
        public string TagsText => string.Join(", ", TagOptions.Where(tag => tag.IsAssigned).Select(tag => tag.Name));

        /// <summary>
        /// Gets the existing tags that can be assigned to this file.
        /// </summary>
        public ObservableCollection<TagAssignmentViewModel> TagOptions { get; } = [];

        /// <summary>
        /// Gets the command that removes this entry from the in-memory catalog.
        /// </summary>
        public ICommand RemoveCommand { get; }

        /// <summary>
        /// Adds an available tag option, reflecting whether the file already has that tag.
        /// </summary>
        public void AddAvailableTag(string tag)
        {
            if (TagOptions.Any(option => string.Equals(option.Name, tag, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var isAssigned = Entry.Tags!.Contains(tag, StringComparer.OrdinalIgnoreCase);
            TagOptions.Add(new TagAssignmentViewModel(tag, isAssigned, () => OnTagAssignmentChanged(tag)));
        }

        /// <summary>
        /// Assigns an existing tag to this file.
        /// </summary>
        public void AssignTag(string tag)
        {
            AddAvailableTag(tag);
            var option = TagOptions.FirstOrDefault(item => string.Equals(item.Name, tag, StringComparison.OrdinalIgnoreCase));
            if (option is not null)
            {
                option.IsAssigned = true;
            }
        }

        /// <summary>
        /// Adds or removes the changed tag in the persisted entry and refreshes dependent views.
        /// </summary>
        private void OnTagAssignmentChanged(string tag)
        {
            if (TagOptions.First(option => string.Equals(option.Name, tag, StringComparison.OrdinalIgnoreCase)).IsAssigned)
            {
                if (!Entry.Tags!.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    Entry.Tags.Add(tag);
                }
            }
            else
            {
                Entry.Tags!.RemoveAll(existingTag => string.Equals(existingTag, tag, StringComparison.OrdinalIgnoreCase));
            }

            OnPropertyChanged(nameof(TagsText));
            _tagsChanged();
        }

        /// <summary>
        /// Raises the property-changed event for WPF bindings.
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
