using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace JBSnorro
{
	/// <summary>
	/// Wraps around the event <see cref="ProcessExit"/>.
	/// </summary>
	public static class ProcessExit
	{
		private static readonly object handlersListLock = new object();

		//prevents garbage collection of the handlers
		private static readonly List<EventHandler> handlers = new List<EventHandler>();
		/// <summary>
		/// This event is triggered when the application exists (even when the close button is clicked, as opposed to <see cref="AppDomain.ProcessExit"/>). 
		/// </summary>
		public static event EventHandler Event
		{
			add
			{
				lock (handlersListLock)
				{
					handlers.Add(value);
					SetConsoleCtrlHandler(value, true);
				}
			}
			remove
			{
				lock (handlersListLock)
				{
					handlers.Remove(value);
					SetConsoleCtrlHandler(value, false);
				}
			}
		}

		[DllImport("Kernel32")]
		private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);


		/// <summary>
		/// The signature of a native ProcessExit event.
		/// </summary>
		/// <returns> true if the function succeeded; false otherwise. </returns>
		public delegate bool EventHandler(EventArg sig);

		/// <summary>
		/// The argument to a native ProcessExit event handler.
		/// </summary>
		public enum EventArg
		{
			/// <summary>
			/// Indicates the console closes because ctrl+c was pressed.
			/// </summary>
			Control_C = 0,
			/// <summary>
			/// Indicates the console closes because to ctrl+break was pressed.
			/// </summary>
			Control_Break = 1,
			/// <summary>
			/// Indicates the console closes because to close button was clicked.
			/// </summary>
			Close = 2,
			/// <summary>
			/// Indicates the console closes because the user logged off. The process exit handler will not get called in this case.
			/// </summary>
			LogOff = 5,
			/// <summary>
			/// Indicates the console closes because a system shutdown command was issued. The process exit handler will not get called in this case.
			/// </summary>
			Shutdown = 6
		}
	}
}
