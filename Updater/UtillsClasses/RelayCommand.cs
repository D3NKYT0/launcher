using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Updater.UtillsClasses
{
	public class RelayCommand : ICommand
	{
		private readonly Action<object?> _execute;

		private readonly Predicate<object?>? _canExecute;

		public event EventHandler? CanExecuteChanged
		{
			add
			{
				CommandManager.RequerySuggested += value;
			}
			remove
			{
				CommandManager.RequerySuggested -= value;
			}
		}

		public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
		{
			if (execute == null)
			{
				throw new ArgumentNullException("execute");
			}
			_execute = execute;
			_canExecute = canExecute;
		}

		[DebuggerStepThrough]
		public bool CanExecute(object? parameter)
		{
			return _canExecute == null || _canExecute(parameter);
		}

		public void Execute(object? parameter = null)
		{
			_execute(parameter);
		}
	}
}
