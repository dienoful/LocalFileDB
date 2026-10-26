using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LocalFileDB.Commands;
using LocalFileDB.Models;
using LocalFileDB.Services;

namespace LocalFileDB.ViewModels
{
    /// <summary>
    /// Exposes one catalog entry and its current file-system metadata to the views.
    /// </summary>
    public sealed class FileEntryViewModel : INotifyPropertyChanged
    {
        private readonly Action _tagsChanged;
        private bool _exists;
        private long? _sizeBytes;
        private DateTime? _lastModified;

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

            RefreshSystemMetadata();
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
        /// Gets whether the resolved path currently points to an existing file.
        /// </summary>
        public bool Exists => _exists;

        /// <summary>
        /// Gets the status label shown in the table.
        /// </summary>
        public string StatusText => Exists ? "Available" : "Missing";

        /// <summary>
        /// Gets the current file size in bytes for numeric sorting.
        /// </summary>
        public long? SizeBytes => _sizeBytes;

        /// <summary>
        /// Gets a human-readable file size for display.
        /// </summary>
        public string SizeText => _sizeBytes is null ? "—" : FormatSize(_sizeBytes.Value);

        /// <summary>
        /// Gets the current last-write time for sorting.
        /// </summary>
        public DateTime? LastModified => _lastModified;

        /// <summary>
        /// Gets a formatted last-write time for display.
        /// </summary>
        public string LastModifiedText => _lastModified?.ToString("g", CultureInfo.CurrentCulture) ?? "—";

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
        /// Refreshes availability, size, and last-write time from the file system.
        /// </summary>
        public void RefreshSystemMetadata()
        {
            try
            {
                var file = new FileInfo(ResolvedPath);
                file.Refresh();
                SetMetadata(file.Exists, file.Exists ? file.Length : null, file.Exists ? file.LastWriteTime : null);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                SetMetadata(false, null, null);
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
        /// Updates the non-persisted file metadata and notifies the table when it changes.
        /// </summary>
        private void SetMetadata(bool exists, long? sizeBytes, DateTime? lastModified)
        {
            var statusChanged = _exists != exists;
            var sizeChanged = _sizeBytes != sizeBytes;
            var modifiedChanged = _lastModified != lastModified;

            _exists = exists;
            _sizeBytes = sizeBytes;
            _lastModified = lastModified;

            if (statusChanged)
            {
                OnPropertyChanged(nameof(Exists));
                OnPropertyChanged(nameof(StatusText));
            }

            if (sizeChanged)
            {
                OnPropertyChanged(nameof(SizeBytes));
                OnPropertyChanged(nameof(SizeText));
            }

            if (modifiedChanged)
            {
                OnPropertyChanged(nameof(LastModified));
                OnPropertyChanged(nameof(LastModifiedText));
            }
        }

        /// <summary>
        /// Formats a byte count using binary units.
        /// </summary>
        private static string FormatSize(long sizeBytes)
        {
            var size = (double)sizeBytes;
            var unitIndex = 0;
            var units = new[] { "B", "KB", "MB", "GB", "TB" };

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size.ToString(size < 10 ? "0.##" : "0.#", CultureInfo.CurrentCulture)} {units[unitIndex]}";
        }

        /// <summary>
        /// Raises the property-changed event for WPF bindings.
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
