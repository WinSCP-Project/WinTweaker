using System;
using System.Windows.Input;

namespace WinTweaker.ViewModels
{
    public sealed class RelayCommand : ICommand
    {
        readonly Action<object> _run;
        readonly Func<object, bool> _can;

        public RelayCommand(Action<object> run, Func<object, bool> can)
        {
            _run = run;
            _can = can;
        }

        public RelayCommand(Action<object> run) : this(run, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            return _can == null || _can(parameter);
        }

        public void Execute(object parameter)
        {
            _run(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
