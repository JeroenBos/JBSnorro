using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace JBSnorro.Extensions
{
	/// <summary>
	/// Provides methods for more easily eschewing lambdas or local functions, to which you currently cannot add DebuggerHiddenAttribute.
	/// </summary>
	public static class DebuggerHidden
	{
		[DebuggerHidden]
		public static TResult Invoke<T, TResult>(Func<T, TResult> f, T arg)
		{
			return f(arg);
		}
		[DebuggerHidden]
		public static TResult Invoke<T1, T2, TResult>(Func<T1, T2, TResult> f, T1 arg1, T2 arg2)
		{
			return f(arg1, arg2);
		}

		[DebuggerHidden]
		public static void Invoke<T>(Action<T> f, T arg)
		{
			f(arg);
		}
		[DebuggerHidden]
		public static void Invoke<T1, T2>(Action<T1, T2> f, T1 arg1, T2 arg2)
		{
			f(arg1, arg2);
		}
		[DebuggerHidden]
		public static Action Curry<T>(Action<T> f, T arg)
		{
			return new curryA<T>(f, arg).Invoke;
		}
		[DebuggerHidden]
		public static Func<TResult> Curry<T, TResult>(Func<T, TResult> f, T arg)
		{
			return new curryF<T, TResult>(f, arg).Invoke;
		}
		[DebuggerHidden]
		public static Action<T1> Curry<T1, T2>(Action<T1, T2> f, T2 arg)
		{
			return new curryA<T1, T2>(f, arg).Invoke;
		}
		public static ParameterizedThreadStart CurryParameterizedThreadStart<T2>(Action<object, T2> f, T2 arg)
		{
			return new curryA<object, T2>(f, arg).Invoke;
		}

		[DebuggerHidden]
		public static Action Curry<T1, T2>(Action<T1, T2> f, T1 arg1, T2 arg2)
		{
			Action<T1> curriedOnce = new curryA<T1, T2>(f, arg2).Invoke;
			return Curry<T1>(curriedOnce, arg1).Invoke;
		}
		[DebuggerHidden]
		public static Func<T1, TResult> Curry<T1, T2, TResult>(Func<T1, T2, TResult> f, T2 arg)
		{
			return new curryF<T1, T2, TResult>(f, arg).Invoke;
		}

		[DebuggerHidden]
		public static Action Curry<T1, T2, T3>(Action<T1, T2, T3> f, T1 arg1, T2 arg2, T3 arg3)
		{
			Action<T1, T2> curriedOnce = new curryA<T1, T2, T3>(f, arg3).Invoke;
			return Curry<T1, T2>(curriedOnce, arg1, arg2).Invoke;
		}
		[DebuggerHidden]
		public static Func<T1, T2, TResult> Curry<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> f, T3 arg)
		{
			return new curryF<T1, T2, T3, TResult>(f, arg).Invoke;
		}
	}


	readonly struct curryA<T>
	{
		private readonly Action<T> f;
		private readonly T arg;
		[DebuggerHidden]
		public curryA(Action<T> f, T arg) => (this.f, this.arg) = (f, arg);
		[DebuggerHidden]
		public void Invoke()
		{
			this.f(this.arg);
		}
	}
	readonly struct curryF<T, TResult>
	{
		private readonly Func<T, TResult> f;
		private readonly T arg;
		[DebuggerHidden]
		public curryF(Func<T, TResult> f, T arg) => (this.f, this.arg) = (f, arg);
		[DebuggerHidden]
		public TResult Invoke()
		{
			return this.f(this.arg);
		}
	}
	readonly struct curryA<T1, T2>
	{
		private readonly Action<T1, T2> f;
		private readonly T2 arg;
		[DebuggerHidden]
		public curryA(Action<T1, T2> f, T2 arg) => (this.f, this.arg) = (f, arg);
		[DebuggerHidden]
		public void Invoke(T1 arg)
		{
			this.f(arg, this.arg);
		}
	}

	readonly struct curryF<T1, T2, TResult>
	{
		private readonly Func<T1, T2, TResult> f;
		private readonly T2 arg;
		[DebuggerHidden]
		public curryF(Func<T1, T2, TResult> f, T2 arg) => (this.f, this.arg) = (f, arg);
		[DebuggerHidden]
		public TResult Invoke(T1 arg)
		{
			return this.f(arg, this.arg);
		}
	}

	readonly struct curryA<T1, T2, T3>
	{
		private readonly Action<T1, T2, T3> f;
		private readonly T3 arg;
		[DebuggerHidden]
		public curryA(Action<T1, T2, T3> f, T3 arg) => (this.f, this.arg) = (f, arg);
		[DebuggerHidden]
		public void Invoke(T1 arg1, T2 arg2)
		{
			this.f(arg1, arg2, this.arg);
		}
	}


	readonly struct curryF<T1, T2, T3, TResult>
	{
		private readonly Func<T1, T2, T3, TResult> f;
		private readonly T3 arg;
		[DebuggerHidden]
		public curryF(Func<T1, T2, T3, TResult> f, T3 arg) => (this.f, this.arg) = (f, arg);
		[DebuggerHidden]
		public TResult Invoke(T1 arg1, T2 arg2)
		{
			return this.f(arg1, arg2, this.arg);
		}
	}
}
