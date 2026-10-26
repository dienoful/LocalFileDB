using System.Windows;
using LocalFileDB.Services;
using LocalFileDB.ViewModels;

namespace LocalFileDB.Views
{
    /// <summary>
    /// WPF view that binds the main window to its view model.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes the view and composes its application services.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(
                new FileDatabaseService(),
                new AppSettingsStore(),
                new WindowsDialogService());
        }
    }
}
