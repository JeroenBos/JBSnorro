using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JBSnorro.View
{
	/// <summary>
	/// A queuing mechanism of tasks to be processed, which also tracks which tasks are currently being executed.
	/// For each dequeued task identifier, the method <see cref="OnProcessed(T)"/> must be called, indicating its execution finished.
	/// </summary>
	/// <typeparam name="T"> The type representing a task identifier, i.e. an instance of this type identifies a task to be executed. </typeparam>
	public class ProcessingQueue<T>
	{
		private readonly object _lock = new object();
		private readonly Queue<T> queue = new Queue<T>();
		private readonly List<T> processing = new List<T>();
		private readonly IEqualityComparer<T> idEqualityComparer;
		private readonly Action pulseCallback;
		private int count;

		/// <summary>
		/// Gets the number of enqueued plus currently processing tasks.
		/// </summary>
		public int Count => count;
		/// <summary>
		/// Gets the collection of currently tasks currently being processed.
		/// </summary>
		public IReadOnlyCollection<T> CurrentlyProcessingTasks => processing;

		/// <param name="pulseCallback"> A callback called whenever an action is enqueued and no task was being processed. 
		/// This maybe used to kickstart the processing mechanism, in case it laid dormant. </param>
		/// <param name="idEqualityComparer"> The task identifier equality comparer. Used for setting the status of a task as 'processed' in <see cref="OnProcessed(T)"/>.</param>
		public ProcessingQueue(Action pulseCallback, IEqualityComparer<T> idEqualityComparer = null)
		{
			Contract.Requires(pulseCallback != null);

			this.pulseCallback = pulseCallback;
			this.idEqualityComparer = idEqualityComparer ?? EqualityComparer<T>.Default;
		}

		/// <summary>
		/// Enqueues a task identifier for execution.
		/// </summary>
		public void Enqueue(T taskId)
		{
			bool isFirst;
			lock (_lock)
			{
				isFirst = this.Count == 0;
				this.queue.Enqueue(taskId);
				this.count++;
			}
			if (isFirst)
			{
				this.pulseCallback();
			}
		}
		/// <summary>
		/// Tries to obtain a task identifier for execution. Returns whether a task identifier was returned; otherwise this queue is empty.
		/// </summary>
		public bool TryDequeue(out T taskId)
		{
			lock (_lock)
			{
				if (queue.Count == 0)
				{
					taskId = default;
					return false;
				}
				taskId = queue.Dequeue();
				this.processing.Add(taskId);
				return true;
			}
		}
		/// <summary>
		/// Signals that the task identified by the specified id finished exeuction. Throws if that task was not currently processing.
		/// </summary>
		public void OnProcessed(T taskId)
		{
			lock (_lock)
			{
				int index = this.processing.IndexOf(taskId, this.idEqualityComparer);
				if (index == -1)
				{
					throw new InvalidOperationException("The specified item was not a currently running task");
				}
				count--;
			}
		}
	}
}
