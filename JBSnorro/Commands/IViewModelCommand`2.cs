using JBSnorro.Diagnostics;
using System.Diagnostics;

namespace JBSnorro.Commands;

public interface IViewModelCommand
{
	void Execute(object viewModel, object eventArgs);
	bool CanExecute(object viewModel, object eventArgs);

}
public interface IViewModelCommand<in TViewModel, in TEventArgs> : IViewModelCommand
{
	void Execute(TViewModel viewModel, TEventArgs eventArgs);
	bool CanExecute(TViewModel viewModel, TEventArgs eventArgs);
}

public class DelegateViewModelCommand<TViewModel, TEventArgs> : IViewModelCommand<TViewModel, TEventArgs>, INamedCommand
{
	private readonly Action<TViewModel, TEventArgs> execute;
	private readonly Func<TViewModel, TEventArgs, bool> canExecute;
	public string? CommandName { get; }

	public DelegateViewModelCommand(Action<TViewModel, TEventArgs> execute,
									Func<TViewModel, TEventArgs, bool>? canExecute = null,
									string? commandName = null)
	{
		Contract.Requires(execute != null);

		this.execute = execute;
		this.canExecute = canExecute ?? ((_, __) => true);
		this.CommandName = commandName;
	}

	public void Execute(TViewModel viewModel, TEventArgs eventArgs) => execute(viewModel, eventArgs);
	public bool CanExecute(TViewModel viewModel, TEventArgs eventArgs) => canExecute(viewModel, eventArgs);
	bool IViewModelCommand.CanExecute(object viewModel, object eventArgs) => this.RedirectCanExecute(viewModel, eventArgs);
	void IViewModelCommand.Execute(object viewModel, object eventArgs) => this.RedirectExecute(viewModel, eventArgs);
}


public static class ViewModelCommandExtensions
{
	/// <summary>
	/// A default implementation for <see cref="IViewModelCommand.Execute(object, object)"/> that delegates to the generic interface 'override' <see cref="IViewModelCommand{TViewModel, TEventArgs}.Execute(TViewModel, TEventArgs)"/>
	/// </summary>
	[DebuggerHidden]
	public static bool RedirectCanExecute<TViewModel, TEventArgs>(
		this IViewModelCommand<TViewModel, TEventArgs> @this,
		object viewModel,
		object eventArgs)
	{
		Contract.Requires(@this != null);
		Contract.Requires(viewModel != null);
		Contract.Requires(eventArgs != null);

		var (tViewModel, tEventArgs) = castHelper<TViewModel, TEventArgs>(viewModel, eventArgs, @this.getCommandName());
		return @this.CanExecute(tViewModel, tEventArgs);
	}

	/// <summary>
	/// A default implementation for <see cref="IViewModelCommand.Execute(object, object)"/> that delegates to the generic interface 'override' <see cref="IViewModelCommand{TViewModel, TEventArgs}.Execute(TViewModel, TEventArgs)"/>
	/// </summary>
	[DebuggerHidden]
	public static void RedirectExecute<TViewModel, TEventArgs>(
		this IViewModelCommand<TViewModel, TEventArgs> @this,
		object viewModel,
		object eventArgs)
	{
		Contract.Requires(@this != null);
		Contract.Requires(viewModel != null);
		Contract.Requires(eventArgs != null);

		var (tViewModel, tEventArgs) = castHelper<TViewModel, TEventArgs>(viewModel, eventArgs, @this.getCommandName());
		@this.Execute(tViewModel, tEventArgs);
	}

	[DebuggerHidden]
	internal static (TViewModel, TEventArgs) castHelper<TViewModel, TEventArgs>(object viewModel, object eventArgs, string commandName)
	{
		string formattedCommandName = commandName == null ? "" : $"'{commandName}' ";

		if (!(viewModel is TViewModel tViewModel))
		{
			throw new InvalidOperationException(
				$"The command {formattedCommandName}cannot be executed on a viewmodel of type '{viewModel.GetType()}'. "
			  + $"Expected type '{typeof(TViewModel).Name}'");
		}

		if (!(eventArgs is TEventArgs tEventArgs))
		{
			throw new InvalidOperationException(
				$"The command {formattedCommandName}cannot be executed with event args of type '{eventArgs.GetType()}'. "
			  + $"Expected type '{typeof(TEventArgs).Name}'");
		}

		return (tViewModel, tEventArgs);
	}

	internal static string getCommandName(this object command)
	{
		Contract.Requires(command != null);

		if (command is INamedCommand namedCommand && !string.IsNullOrEmpty(namedCommand.CommandName))
		{
			return namedCommand.CommandName;
		}

		var name = command.ToString();
		Contract.Assert(name != null);
		if (name != command.GetType().ToString())
			return name;
		if (name.Contains("`"))
			name = name.Substring(0, name.IndexOf('`')); // remove generic type parameter
		if (name.Contains("."))
			name = name.Substring(name.LastIndexOf('.') + 1); // remove qualifiers
		if (name.EndsWith("Command"))
			name = name.Substring(0, name.Length - "Command".Length); // remove postfix "Command"
		return name;
	}

	/// <summary>
	/// Wraps a command with a function that maps one view model onto another.
	/// </summary>
	[DebuggerHidden]
	public static IViewModelCommand<TSourceViewModel, TEventArgs> Map<TSourceViewModel, TResultViewModel, TEventArgs>(
		this IViewModelCommand<TResultViewModel, TEventArgs> source,
		Func<TSourceViewModel, TResultViewModel> viewModelMap)
	{
		return Map<TSourceViewModel, TResultViewModel, TEventArgs, TEventArgs>(source, viewModelMap, _ => _);
	}

	/// <summary>
	/// Wraps a command with a function that maps one view model event arg onto another.
	/// </summary>
	[DebuggerHidden]
	public static IViewModelCommand<TViewModel, TSourceEventArgs> Map<TViewModel, TResultEventArgs, TSourceEventArgs>(
		this IViewModelCommand<TViewModel, TResultEventArgs> source,
		Func<TSourceEventArgs, TResultEventArgs> eventArgsMap)
	{
		return Map<TViewModel, TViewModel, TSourceEventArgs, TResultEventArgs>(source, _ => _, eventArgsMap);
	}


	/// <summary>
	/// Wraps a command with a function that maps one view model onto another.
	/// </summary>
	public static IViewModelCommand<TSourceViewModel, TSourceEventArgs> Map<TSourceViewModel, TResultViewModel, TSourceEventArgs, TResultEventArgs>(
		this IViewModelCommand<TResultViewModel, TResultEventArgs> source,
		Func<TSourceViewModel, TResultViewModel> viewModelMap,
		Func<TSourceEventArgs, TResultEventArgs> eventArgsMap)
	{
		return Map<TSourceViewModel, TResultViewModel, TSourceEventArgs, TResultEventArgs>(source, (sender, e) => viewModelMap(sender), eventArgsMap);
	}



	/// <summary>
	/// Wraps a command with a function that maps one view model onto another.
	/// </summary>
	public static IViewModelCommand<TSourceViewModel, TSourceEventArgs> Map<TSourceViewModel, TResultViewModel, TSourceEventArgs, TResultEventArgs>(
		this IViewModelCommand<TResultViewModel, TResultEventArgs> source,
		Func<TSourceViewModel, TSourceEventArgs, TResultViewModel> viewModelMap,
		Func<TSourceEventArgs, TResultEventArgs> eventArgsMap)
	{
		Contract.Requires(source != null);
		Contract.Requires(viewModelMap != null);
		Contract.Requires(eventArgsMap != null);

		return new DelegateViewModelCommand<TSourceViewModel, TSourceEventArgs>(
			(vm, e) => source.Execute(viewModelMap(vm, e), eventArgsMap(e)),
			(vm, e) => source.CanExecute(viewModelMap(vm, e), eventArgsMap(e)),
			(source as INamedCommand)?.getCommandName()
		);
	}
}
