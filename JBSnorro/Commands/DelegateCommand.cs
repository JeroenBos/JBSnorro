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
	public class DelegateCommand : ICommand
	{
		private readonly Func<bool> canExecute;
		private readonly Action execute;

		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action execute)
			: this(execute, null)
		{
		}

		public DelegateCommand(Action execute, Func<bool> canExecute)
		{
			this.execute = execute;
			this.canExecute = canExecute;
		}

		public virtual bool CanExecute()
		{
			if (canExecute == null)
			{
				return true;
			}

			return canExecute();
		}
		[DebuggerHidden]
		public virtual void Execute()
		{
			execute();
		}

		public virtual void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

		bool ICommand.CanExecute(object parameter)
		{
			Contract.Requires(parameter == null, "The specified parameter is neglected");

			return CanExecute();
		}
		void ICommand.Execute(object parameter)
		{
			Contract.Requires(parameter == null, "The specified parameter is neglected");

			Execute();
		}
	}
}
