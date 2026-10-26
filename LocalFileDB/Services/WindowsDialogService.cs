using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace LocalFileDB.Services
{
    /// <summary>
    /// Shows native Windows file and folder dialogs and user messages.
    /// </summary>
    public sealed class WindowsDialogService
    {
        /// <summary>
        /// Prompts the user to select a JSON database.
        /// </summary>
        public string? SelectDatabaseFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open database",
                Filter = "JSON database (*.json)|*.json",
                DefaultExt = ".json",
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <summary>
        /// Prompts the user to select one or more catalog files.
        /// </summary>
        public IReadOnlyList<string> SelectFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Add files to database",
                Filter = "All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = true
            };

            return dialog.ShowDialog() == true ? dialog.FileNames : [];
        }

        /// <summary>
        /// Prompts the user to select a root directory.
        /// </summary>
        public string? SelectRootDirectory(string? initialDirectory)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select root directory",
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        /// <summary>
        /// Shows an error message without exposing WPF dialog types to a ViewModel.
        /// </summary>
        public void ShowError(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Shows a warning message without exposing WPF dialog types to a ViewModel.
        /// </summary>
        public void ShowWarning(string message, string title) =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
