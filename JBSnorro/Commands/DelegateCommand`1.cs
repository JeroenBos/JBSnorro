using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JBSnorro.Commands
{
	public class DelegateCommand<T> : ICommand
	{
		private readonly Func<T, bool> canExecute;
		private readonly Action<T> execute;

		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action<T> execute)
			: this(execute, null)
		{
		}

		public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
		{
			this.execute = execute;
			this.canExecute = canExecute;
		}

		public virtual bool CanExecute(T parameter)
		{
			if (canExecute == null)
			{
				return true;
			}

			return canExecute(parameter);
		}
		[DebuggerHidden]
		public virtual void Execute(T parameter)
		{
			execute(parameter);
		}

		public virtual void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

		bool ICommand.CanExecute(object parameter)
		{
			T parameter_T = this.Cast(parameter);

			return CanExecute(parameter_T);
		}

		void ICommand.Execute(object parameter)
		{
			T parameter_T = this.Cast(parameter);

			Execute(parameter_T);
		}

		protected virtual T Cast(object parameter)
		{
			if (parameter != null)
				Contract.Requires(parameter is T, $"Parameter to command of wrong type: expected {typeof(T)}, received {parameter.GetType()}");

			return (T)parameter;
		}
	}
}
