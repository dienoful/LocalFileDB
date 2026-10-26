using System.Windows.Input;

namespace LocalFileDB.Commands
{
    /// <summary>
    /// Adapts delegates to WPF's command interface without an MVVM framework.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        /// <summary>
        /// Creates a command with an action and an optional availability check.
        /// </summary>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Raised when the command's availability changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Returns whether the command can run for the supplied parameter.
        /// </summary>
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <summary>
        /// Runs the command action.
        /// </summary>
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>
        /// Notifies WPF that the command's availability may have changed.
        /// </summary>
        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
