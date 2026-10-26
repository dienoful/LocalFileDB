using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LocalFileDB.Commands;
using LocalFileDB.Models;
using LocalFileDB.Services;

namespace LocalFileDB.ViewModels
{
    /// <summary>
    /// Owns the main window state and commands for browsing a file catalog.
    /// </summary>
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly FileDatabaseService _databaseService;
        private readonly AppSettingsStore _settingsStore;
        private readonly WindowsDialogService _dialogService;
        private List<string> _recentDatabasePaths = [];
        private string? _databaseFilePath;
        private string _databaseName = string.Empty;
        private string _rootDirectory = string.Empty;
        private string _fileNameFilter = string.Empty;
        private string _newTagName = string.Empty;
        private FileEntryViewModel? _selectedFile;
        private BitmapSource? _previewImage;
        private string _previewMessage = "Select a file to preview.";
        private int _previewRequestId;

        /// <summary>
        /// Creates the window state and loads the saved recent database list.
        /// </summary>
        public MainWindowViewModel(
            FileDatabaseService databaseService,
            AppSettingsStore settingsStore,
            WindowsDialogService dialogService)
        {
            _databaseService = databaseService;
            _settingsStore = settingsStore;
            _dialogService = dialogService;
            _recentDatabasePaths = _settingsStore.LoadRecentDatabasePaths();

            OpenDatabaseCommand = new RelayCommand(_ => OpenDatabaseFromDialog());
            AddFilesCommand = new RelayCommand(_ => AddFiles(), _ => IsDatabaseOpen);
            SaveDatabaseCommand = new RelayCommand(_ => SaveDatabase(), _ => IsDatabaseOpen);
            RemoveSelectedFileCommand = new RelayCommand(_ => RemoveSelectedFile(), _ => SelectedFile is not null);
            BrowseRootDirectoryCommand = new RelayCommand(_ => BrowseRootDirectory(), _ => IsDatabaseOpen);
            AddTagCommand = new RelayCommand(_ => AddTagToSelectedFile(), _ => SelectedFile is not null && !string.IsNullOrWhiteSpace(NewTagName));

            RefreshRecentDatabases();
        }

        /// <summary>
        /// Raised when a bindable property changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the title displayed by the main window.
        /// </summary>
        public string WindowTitle => string.IsNullOrWhiteSpace(DatabaseName)
            ? "LocalFileDB"
            : $"{DatabaseName} - LocalFileDB";

        /// <summary>
        /// Gets or sets the editable catalog name.
        /// </summary>
        public string DatabaseName
        {
            get => _databaseName;
            set
            {
                if (SetProperty(ref _databaseName, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        /// <summary>
        /// Gets or sets the catalog's root directory.
        /// </summary>
        public string RootDirectory
        {
            get => _rootDirectory;
            private set => SetProperty(ref _rootDirectory, value);
        }

        /// <summary>
        /// Gets the path of the currently opened database.
        /// </summary>
        public string DatabaseFilePath
        {
            get => _databaseFilePath ?? string.Empty;
            private set
            {
                if (SetProperty(ref _databaseFilePath, value))
                {
                    OnPropertyChanged(nameof(IsDatabaseOpen));
                    AddFilesCommand.NotifyCanExecuteChanged();
                    SaveDatabaseCommand.NotifyCanExecuteChanged();
                    BrowseRootDirectoryCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets whether a database is currently open.
        /// </summary>
        public bool IsDatabaseOpen => _databaseFilePath is not null;

        /// <summary>
        /// Gets whether a file is selected in the catalog.
        /// </summary>
        public bool HasSelectedFile => SelectedFile is not null;

        /// <summary>
        /// Gets the label describing the current tag-filter selection.
        /// </summary>
        public string TagFilterSummary
        {
            get
            {
                var selectedCount = AvailableTags.Count(tag => tag.IsSelected);
                return selectedCount == 0 ? "All tags" : $"{selectedCount} selected";
            }
        }

        /// <summary>
        /// Gets or sets the name of a new tag to add to the selected file.
        /// </summary>
        public string NewTagName
        {
            get => _newTagName;
            set
            {
                if (SetProperty(ref _newTagName, value))
                {
                    AddTagCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the case-insensitive file-name search text.
        /// </summary>
        public string FileNameFilter
        {
            get => _fileNameFilter;
            set
            {
                if (SetProperty(ref _fileNameFilter, value))
                {
                    RefreshDisplayedFiles();
                }
            }
        }

        /// <summary>
        /// Gets the catalog entries shown in the table.
        /// </summary>
        public ObservableCollection<FileEntryViewModel> Files { get; } = [];

        /// <summary>
        /// Gets the entries currently visible after applying the active filters.
        /// </summary>
        public ObservableCollection<FileEntryViewModel> DisplayedFiles { get; } = [];

        /// <summary>
        /// Gets unique tags found in the currently loaded catalog.
        /// </summary>
        public ObservableCollection<TagFilterItemViewModel> AvailableTags { get; } = [];

        /// <summary>
        /// Gets the databases shown in the recent databases menu.
        /// </summary>
        public ObservableCollection<RecentDatabaseViewModel> RecentDatabases { get; } = [];

        /// <summary>
        /// Gets or sets the currently selected catalog entry.
        /// </summary>
        public FileEntryViewModel? SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (SetProperty(ref _selectedFile, value))
                {
                    OnPropertyChanged(nameof(HasSelectedFile));
                    RemoveSelectedFileCommand.NotifyCanExecuteChanged();
                    AddTagCommand.NotifyCanExecuteChanged();
                    _ = UpdatePreviewAsync(value);
                }
            }
        }

        /// <summary>
        /// Gets the image displayed in the preview panel, when available.
        /// </summary>
        public BitmapSource? PreviewImage
        {
            get => _previewImage;
            private set => SetProperty(ref _previewImage, value);
        }

        /// <summary>
        /// Gets the current preview status or fallback message.
        /// </summary>
        public string PreviewMessage
        {
            get => _previewMessage;
            private set => SetProperty(ref _previewMessage, value);
        }

        /// <summary>
        /// Gets the command that opens a database file.
        /// </summary>
        public RelayCommand OpenDatabaseCommand { get; }

        /// <summary>
        /// Gets the command that adds files to the open database.
        /// </summary>
        public RelayCommand AddFilesCommand { get; }

        /// <summary>
        /// Gets the command that saves the open database.
        /// </summary>
        public RelayCommand SaveDatabaseCommand { get; }

        /// <summary>
        /// Gets the command that removes the selected database entry.
        /// </summary>
        public RelayCommand RemoveSelectedFileCommand { get; }

        /// <summary>
        /// Gets the command that selects a new root directory.
        /// </summary>
        public RelayCommand BrowseRootDirectoryCommand { get; }

        /// <summary>
        /// Gets the command that creates or assigns a tag to the selected file.
        /// </summary>
        public RelayCommand AddTagCommand { get; }

        /// <summary>
        /// Opens the file picker and loads the chosen database.
        /// </summary>
        private void OpenDatabaseFromDialog()
        {
            var path = _dialogService.SelectDatabaseFile();
            if (path is not null)
            {
                OpenDatabase(path);
            }
        }

        /// <summary>
        /// Loads a database, updates the screen, and remembers its path.
        /// </summary>
        private void OpenDatabase(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var database = _databaseService.Load(fullPath);
                var entries = database.Files ?? [];
                var databaseTags = entries
                    .SelectMany(entry => entry.Tags ?? [])
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var fileItems = entries
                    .Select(entry => CreateFileItem(entry, database.RootDirectory, databaseTags))
                    .ToList();

                SelectedFile = null;
                FileNameFilter = string.Empty;
                Files.Clear();
                foreach (var fileItem in fileItems)
                {
                    Files.Add(fileItem);
                }
                RefreshAvailableTags(preserveSelection: false);

                DatabaseFilePath = fullPath;
                DatabaseName = database.Name;
                RootDirectory = database.RootDirectory;
                _recentDatabasePaths = AppSettingsStore.AddRecentDatabase(_recentDatabasePaths, fullPath);
                SaveRecentDatabases();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException)
            {
                _dialogService.ShowError($"Could not open the database.{Environment.NewLine}{Environment.NewLine}{exception.Message}", "Open database");
            }
        }

        /// <summary>
        /// Creates a display item and connects its context-menu command to this view model.
        /// </summary>
        private FileEntryViewModel CreateFileItem(
            FileDatabaseEntry entry,
            string rootDirectory,
            IEnumerable<string>? availableTags = null) =>
            new(entry, rootDirectory, availableTags ?? AvailableTags.Select(tag => tag.Name), RemoveFile, () => RefreshAvailableTags());

        /// <summary>
        /// Rebuilds recent menu items from the stored database paths.
        /// </summary>
        private void RefreshRecentDatabases()
        {
            RecentDatabases.Clear();
            foreach (var path in _recentDatabasePaths)
            {
                RecentDatabases.Add(new RecentDatabaseViewModel(path, OpenDatabase));
            }
        }

        /// <summary>
        /// Persists the current recent database list and reports settings write failures.
        /// </summary>
        private void SaveRecentDatabases()
        {
            try
            {
                _settingsStore.SaveRecentDatabasePaths(_recentDatabasePaths);
                RefreshRecentDatabases();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                _dialogService.ShowWarning(
                    $"The database was opened, but the recent database list could not be saved.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                    "Application settings");
            }
        }

        /// <summary>
        /// Opens the multi-select picker and adds the chosen files to the current catalog.
        /// </summary>
        private void AddFiles()
        {
            try
            {
                var newFiles = _dialogService.SelectFiles()
                    .Select(path => CreateFileItem(_databaseService.CreateEntry(path, RootDirectory), RootDirectory))
                    .ToList();

                foreach (var file in newFiles)
                {
                    Files.Add(file);
                }
                RefreshAvailableTags();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                _dialogService.ShowError($"Could not add the selected files.{Environment.NewLine}{Environment.NewLine}{exception.Message}", "Add files");
            }
        }

        /// <summary>
        /// Prompts for a new root directory and refreshes relative file paths.
        /// </summary>
        private void BrowseRootDirectory()
        {
            var selectedPath = _dialogService.SelectRootDirectory(RootDirectory);
            if (selectedPath is null)
            {
                return;
            }

            RootDirectory = selectedPath;
            RefreshFileItems();
        }

        /// <summary>
        /// Saves the edited name, root directory, and all current file entries.
        /// </summary>
        private void SaveDatabase()
        {
            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                _dialogService.ShowWarning("Enter a database name before saving.", "Save database");
                return;
            }

            var database = new FileDatabase
            {
                Name = DatabaseName.Trim(),
                RootDirectory = RootDirectory,
                Files = Files.Select(file => file.Entry).ToList()
            };

            try
            {
                _databaseService.Save(DatabaseFilePath, database);
                DatabaseName = database.Name;
                RefreshFileItems();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Text.Json.JsonException)
            {
                _dialogService.ShowError($"Could not save the database.{Environment.NewLine}{Environment.NewLine}{exception.Message}", "Save database");
            }
        }

        /// <summary>
        /// Removes the selected entry from the current in-memory catalog.
        /// </summary>
        private void RemoveSelectedFile()
        {
            if (SelectedFile is not null)
            {
                RemoveFile(SelectedFile);
            }
        }

        /// <summary>
        /// Adds a new catalog tag when needed and assigns it to the selected file.
        /// </summary>
        private void AddTagToSelectedFile()
        {
            if (SelectedFile is null || string.IsNullOrWhiteSpace(NewTagName))
            {
                return;
            }

            var tag = NewTagName.Trim();
            var existingTag = AvailableTags.FirstOrDefault(
                option => string.Equals(option.Name, tag, StringComparison.OrdinalIgnoreCase));
            if (existingTag is not null)
            {
                tag = existingTag.Name;
            }
            else
            {
                foreach (var file in Files)
                {
                    file.AddAvailableTag(tag);
                }
            }

            SelectedFile.AssignTag(tag);
            NewTagName = string.Empty;
        }

        /// <summary>
        /// Removes one entry and clears the selection when it was selected.
        /// </summary>
        private void RemoveFile(FileEntryViewModel fileItem)
        {
            if (ReferenceEquals(SelectedFile, fileItem))
            {
                SelectedFile = null;
            }

            Files.Remove(fileItem);
            RefreshAvailableTags();
        }

        /// <summary>
        /// Rebuilds list items so relative paths reflect the current root directory.
        /// </summary>
        private void RefreshFileItems()
        {
            var entries = Files.Select(file => file.Entry).ToList();
            SelectedFile = null;
            Files.Clear();
            foreach (var entry in entries)
            {
                Files.Add(CreateFileItem(entry, RootDirectory));
            }
            RefreshAvailableTags();
        }

        /// <summary>
        /// Applies the file-name and OR-based tag filters to the visible list.
        /// </summary>
        private void RefreshDisplayedFiles()
        {
            var selectedTags = AvailableTags
                .Where(tag => tag.IsSelected)
                .Select(tag => tag.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matchingFiles = Files
                .Where(file => (string.IsNullOrWhiteSpace(FileNameFilter)
                        || file.FileName.Contains(FileNameFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                    && (selectedTags.Count == 0
                        || (file.Entry.Tags ?? []).Any(tag => selectedTags.Contains(tag))))
                .ToList();

            var selectedFile = SelectedFile;
            for (var index = DisplayedFiles.Count - 1; index >= 0; index--)
            {
                if (!matchingFiles.Contains(DisplayedFiles[index]))
                {
                    DisplayedFiles.RemoveAt(index);
                }
            }

            for (var index = 0; index < matchingFiles.Count; index++)
            {
                var file = matchingFiles[index];
                if (index < DisplayedFiles.Count && ReferenceEquals(DisplayedFiles[index], file))
                {
                    continue;
                }

                var currentIndex = DisplayedFiles.IndexOf(file);
                if (currentIndex >= 0)
                {
                    DisplayedFiles.Move(currentIndex, index);
                }
                else
                {
                    DisplayedFiles.Insert(index, file);
                }
            }

            if (selectedFile is not null && !matchingFiles.Contains(selectedFile))
            {
                SelectedFile = null;
            }
        }

        /// <summary>
        /// Rebuilds the tag options from catalog entries and optionally keeps the current selection.
        /// </summary>
        private void RefreshAvailableTags(bool preserveSelection = true)
        {
            var selectedTags = preserveSelection
                ? AvailableTags.Where(tag => tag.IsSelected).Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tags = Files
                .SelectMany(file => file.Entry.Tags ?? [])
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            AvailableTags.Clear();
            foreach (var tag in tags)
            {
                AvailableTags.Add(new TagFilterItemViewModel(
                    tag,
                    selectedTags.Contains(tag),
                    OnTagFilterSelectionChanged));
            }

            OnPropertyChanged(nameof(TagFilterSummary));
            RefreshDisplayedFiles();
        }

        /// <summary>
        /// Refreshes the displayed files when a tag is checked or unchecked.
        /// </summary>
        private void OnTagFilterSelectionChanged()
        {
            OnPropertyChanged(nameof(TagFilterSummary));
            RefreshDisplayedFiles();
        }

        /// <summary>
        /// Loads an image or Windows Shell video thumbnail for the selected entry.
        /// </summary>
        private async Task UpdatePreviewAsync(FileEntryViewModel? fileItem)
        {
            var requestId = ++_previewRequestId;
            PreviewImage = null;
            PreviewMessage = fileItem is null ? "Select a file to preview." : string.Empty;

            if (fileItem is null)
            {
                return;
            }

            if (!File.Exists(fileItem.ResolvedPath))
            {
                PreviewMessage = "The file could not be found.";
                return;
            }

            if (string.Equals(fileItem.MediaType, "video", StringComparison.OrdinalIgnoreCase))
            {
                PreviewMessage = "Loading video preview...";
                try
                {
                    var thumbnail = await Task.Run(() =>
                        WindowsThumbnailProvider.TryGetThumbnail(fileItem.ResolvedPath, 1000, 700));

                    if (requestId != _previewRequestId || !ReferenceEquals(SelectedFile, fileItem))
                    {
                        return;
                    }

                    PreviewImage = thumbnail;
                    PreviewMessage = thumbnail is null
                        ? "Windows could not provide a video thumbnail for this file."
                        : string.Empty;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or InvalidOperationException or System.Runtime.InteropServices.ExternalException)
                {
                    if (requestId == _previewRequestId)
                    {
                        PreviewMessage = "The video preview could not be loaded.";
                    }
                }

                return;
            }

            if (!string.Equals(fileItem.MediaType, "image", StringComparison.OrdinalIgnoreCase))
            {
                PreviewMessage = "Image preview is not available for this file type.";
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 1000;
                bitmap.UriSource = new Uri(fileItem.ResolvedPath, UriKind.Absolute);
                bitmap.EndInit();
                PreviewImage = bitmap;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or FormatException or InvalidOperationException)
            {
                PreviewMessage = "The image preview could not be loaded.";
            }
        }

        /// <summary>
        /// Assigns a new value and raises the binding notification when it changed.
        /// </summary>
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the property-changed event for WPF bindings.
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
