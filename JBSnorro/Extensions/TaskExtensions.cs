using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Extensions
{
	/// <summary>
	/// Contains extension methods related to <see cref="System.Threading.Tasks.Task"/>.
	/// </summary>
	public static class TaskExtensions
	{
		/// <summary> 
		/// Casts a <see cref="Task"/> to a <see cref="Task{TResult}"/>. 
		/// This method will throw an <see cref="InvalidCastException"/> if the specified task 
		/// returns a value which is not identity-convertible to <typeparamref name="T"/>. 
		/// </summary>
		public static async Task<T> Cast<T>(this Task task)
		{
			Contract.Requires(task != null);
			Contract.Requires(task.GetType().IsGenericType && task.GetType().GetGenericTypeDefinition() == typeof(Task<>),
				"An argument of type 'System.Threading.Tasks.Task`1' was expected");

			await task.ConfigureAwait(false);

			object result = task.GetType().GetProperty(nameof(Task<object>.Result)).GetValue(task);
			return (T)result;
		}
		/// <summary> Casts a task returning a <typeparamref name="TSource"/> to a task returning <typeparamref name="TResult"/>.
		/// An upcast is always safe. </summary>
		/// <seealso href="https://stackoverflow.com/a/15530170/308451"/>
		public static Task<TResult> Upcast<TSource, TResult>(this Task<TSource> task) where TSource : TResult
		{
			var tcs = new TaskCompletionSource<TResult>();
			task.ContinueWith(t =>
			{
				if (t.IsFaulted)
					tcs.TrySetException(t.Exception.InnerExceptions);
				else if (t.IsCanceled)
					tcs.TrySetCanceled();
				else
					tcs.TrySetResult(t.Result);
			}, TaskContinuationOptions.ExecuteSynchronously);
			return tcs.Task;
		}
		/// <summary>
		/// Invokes the method or constructor represented by the specified method info, and its result is cast to or wrapped as <see cref="System.Threading.Tasks.Task{Object}"/>.
		/// </summary>
		/// <param name="methodInfo"></param>
		/// <param name="obj">
		/// The object on which to invoke the method or constructor. If a method is static, this argument is ignored. If a constructor is static, this argument must be null
		/// or an instance of the class that defines the constructor. </param>
		/// <param name="arguments">
		/// An argument list for the invoked method or constructor. This is an array of objects with the same number, order, and type as the parameters of the method or constructor
		/// to be invoked. If there are no parameters, parameters should be null. If the method or constructor represented by this instance takes a ref parameter (ByRef
		/// in Visual Basic), no special attribute is required for that parameter in order to invoke the method or constructor using this function. Any object in this array
		/// that is not explicitly initialized with a value will contain the default value for that object type. For reference-type elements, this value is null. For value-type
		/// elements, this value is 0, 0.0, or false, depending on the specific element type.</param>
		public static Task<object> InvokeAsync(this MethodInfo methodInfo, object obj, object[] arguments)
		{
			Contract.Requires(methodInfo != null);
			Contract.Requires(obj != null || methodInfo.IsStatic);
			Contract.Requires(arguments != null);

			var invocationResult = methodInfo.Invoke(obj, arguments);
			if (invocationResult is Task taskResult)
			{
				if (invocationResult.GetType() == typeof(Task))
					return Task.FromResult<object>(null);
				else
					return taskResult.Cast<object>();
			}
			else
			{
				return Task.FromResult(invocationResult);
			}
		}
	}
}
