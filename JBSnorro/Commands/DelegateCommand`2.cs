#nullable disable
using JBSnorro.Diagnostics;

namespace JBSnorro.Commands;

/// <summary> Represents a delegate command that can be invoked by two types of parameters, where one can be mapped to the other. </summary>
public class DelegateCommand<T, U> : DelegateCommand<T>
{
	private readonly Func<U, T> parameterMap;
	/// <summary> Creates a new <see cref="DelegateCommand{T, U}"/>. </summary>
	/// <param name="execute"> The method to be called when this command is invoked. </param>
	/// <param name="canExecute"> The method that determines whether the command can execute in its current state. </param>
	/// <param name="parameterMap"> The method converting a parameter of type <typeparamref name="U"/> to <typeparamref name="T"/>. </param>
	public DelegateCommand(Action<T> execute, Func<T, bool> canExecute, Func<U, T> parameterMap)
		: base(execute, canExecute)
	{
		Contract.Requires(parameterMap != null);

		this.parameterMap = parameterMap;
	}

	public bool CanExecute(U parameter)
	{
		return base.CanExecute(parameterMap(parameter));
	}
	public void Execute(U parameter)
	{
		base.Execute(parameterMap(parameter));
	}

	protected override T Cast(object parameter)
	{
		if (parameter != null)
		{
			Contract.Requires(parameter is U, $"Parameter to command of wrong type: expected {typeof(U)}, received {parameter.GetType()}");
		}
		else
		{
			Contract.Requires(typeof(U).IsByRef, $"Parameter to command of wrong type: could not convert null to {typeof(U)}");
		}

		var parameter_U = (U)parameter;
		var parameter_T = parameterMap(parameter_U);
		return parameter_T;
	}
}
