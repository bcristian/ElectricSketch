using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace ElectricSketch
{
    public sealed class RelayCommand(Action execute, Func<bool> canExecute = null) : ICommand
    {
        readonly Action execute = execute ?? throw new ArgumentNullException(nameof(execute));
        readonly Func<bool> canExecute = canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => canExecute == null || canExecute();

        public void Execute(object parameter) => execute();
    }

    public sealed class RelayCommand<T>(Action<T> execute, Func<T, bool> canExecute = null) : ICommand
    {
        readonly Action<T> execute = execute ?? throw new ArgumentNullException(nameof(execute));
        readonly Func<T, bool> canExecute = canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => canExecute == null || canExecute((T)parameter);

        public void Execute(object parameter) => execute((T)parameter);
    }
}
