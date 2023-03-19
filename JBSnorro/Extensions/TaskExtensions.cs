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
        private const int default_wait_ms = 250;
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
        /// <summary>
        /// Retries the specified delegate on exceptions.
        /// </summary>
        /// <param name="func">The delegate to invoke.</param>
        /// <param name="retryCount">The maximum number of times the delegate is invoked.</param>
        /// <param name="wait_ms">The number of milliseconds to wait in between of invocations.</param>
        /// <returns>The result of awaiting the delegate that didn't fail.</returns>
        public static Task<T> Retry<T>(Func<Task<T>> func, int retryCount = 3, int wait_ms = default_wait_ms)
        {
            return Retry(attempt => func(), retryCount, wait_ms);
        }
        /// <summary>
        /// Retries the specified delegate on exceptions.
        /// </summary>
        /// <param name="func">The delegate to invoke.</param>
        /// <param name="retryCount">The maximum number of times the delegate is invoked.</param>
        /// <param name="wait_ms">The number of milliseconds to wait in between of invocations.</param>
        /// <returns>The result of awaiting the delegate that didn't fail.</returns>
        public static async Task<T> Retry<T>(Func<int, Task<T>> func, int retryCount = 3, int wait_ms = default_wait_ms)
        {
            int i;
            for (i = 0; i < retryCount - 1; i++)
            {
                try
                {
                    return await func(i);
                }
                catch
                {
                    Thread.Sleep(ComputeWait(wait_ms));
                }
            }
            return await func(i);
        }
        /// <summary>
        /// Retries the specified delegate on exceptions.
        /// </summary>
        /// <param name="func">The delegate to invoke.</param>
        /// <param name="retryCount">The maximum number of times the delegate is invoked.</param>
        /// <param name="wait_ms">The number of milliseconds to wait in between of invocations.</param>
        /// <returns>The result of the delegate that didn't fail.</returns>
        public static T Retry<T>(Func<T> func, int retryCount = 3, int wait_ms = default_wait_ms)
        {
            return Retry(attempt => func(), retryCount, wait_ms);
        }
        /// <summary>
        /// Retries the specified delegate on exceptions.
        /// </summary>
        /// <param name="func">The delegate to invoke.</param>
        /// <param name="retryCount">The maximum number of times the delegate is invoked.</param>
        /// <param name="wait_ms">The number of milliseconds to wait in between of invocations.</param>
        /// <returns>The result of the delegate that didn't fail.</returns>
        public static T Retry<T>(Func<int, T> func, int retryCount = 3, int wait_ms = default_wait_ms)
        {
            int i;
            for (i = 0; i < retryCount - 1; i++)
            {
                try
                {
                    return func(i);
                }
                catch
                {
                    Thread.Sleep(ComputeWait(wait_ms));
                }
            }
            return func(i);
        }
        private static int ComputeWait(int wait_ms)
        {
            const int varationSize = 2; // bigger is smaller variation
            var aroundOne = 1 + (Random.Shared.NextSingle() - 0.5) / varationSize;
            return (int)(wait_ms * aroundOne);
        }
        /// <summary>
        /// Retries the specified delegate on exceptions.
        /// </summary>
        /// <param name="func">The delegate to invoke.</param>
        /// <param name="retryCount">The maximum number of times the delegate is invoked.</param>
        /// <param name="wait_ms">The number of milliseconds to wait in between of invocations.</param>
        /// <returns>The result of awaiting the delegate that didn't fail.</returns>
        public static async Task Retry(Func<Task> action, int retryCount = 3, int wait_ms = default_wait_ms)
        {
            await Retry<object>(async () => { await action(); return null; }, retryCount, wait_ms);
        }
        /// <summary>
        /// Retries the specified delegate on exceptions.
        /// </summary>
        /// <param name="func">The delegate to invoke.</param>
        /// <param name="retryCount">The maximum number of times the delegate is invoked.</param>
        /// <param name="wait_ms">The number of milliseconds to wait in between of invocations.</param>
        /// <returns>The result of the delegate that didn't fail.</returns>
        public static void Retry(Action action, int retryCount = 3, int wait_ms = default_wait_ms)
        {
            Retry<object>(() => { action(); return (object)null; }, retryCount, wait_ms);
        }
    }
}
