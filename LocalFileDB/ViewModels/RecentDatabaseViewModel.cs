using System.Windows.Input;
using LocalFileDB.Commands;

namespace LocalFileDB.ViewModels
{
    /// <summary>
    /// Represents one recent database menu item.
    /// </summary>
    public sealed class RecentDatabaseViewModel
    {
        /// <summary>
        /// Creates a recent item that opens its database when invoked.
        /// </summary>
        public RecentDatabaseViewModel(string path, Action<string> openDatabase)
        {
            Path = path;
            DisplayName = System.IO.Path.GetFileNameWithoutExtension(path);
            OpenCommand = new RelayCommand(_ => openDatabase(Path));
        }

        /// <summary>
        /// Gets the full database path used as the menu tooltip.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the short database name shown in the menu.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the command that opens this database.
        /// </summary>
        public ICommand OpenCommand { get; }
    }
}
