using JBSnorro.Diagnostics;
using System.Diagnostics;

namespace JBSnorro;

[DebuggerDisplay("Count = {Count}")]
public sealed class MultipleDependentEnumerable<T> : IDisposable
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly List<IEnumerator<T>> enumerators;
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly Func<int, IEnumerable<T>> getEnumerable;
	/// <summary> Gets the number of sequence in this instance. </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public int Count
	{
		get { return this.enumerators.Count; }
	}

	/// <summary> Creates a new multiple dependent enumerable from the function that fetches the enumerables. </summary>
	/// <param name="getEnumerable"> A function that receives as argument the index of the enumerable that is being requested, which is to be returned. </param>
	public MultipleDependentEnumerable(Func<int, IEnumerable<T>> getEnumerable)
	{
		this.getEnumerable = getEnumerable;
		this.enumerators = new List<IEnumerator<T>>();
	}

	/// <summary> Moves the sequence at the specified index to the next element and removes any sequence after that sequence. 
	/// When no sequence is available at that index, only for the index equal to <code>this.Count</code>, a sequence is fetched from the delegate specified at the construction of this instance. </summary>
	/// <param name="sequenceIndex"> The index of the sequence to move. </param>
	/// <returns> Returns the result of the called <code>IEnumerator&lt;T&gt;.MoveNext</code>. </returns>
	public bool MoveNext(int sequenceIndex)
	{
		Contract.Requires(sequenceIndex >= 0);
		Contract.Requires(sequenceIndex <= this.Count);

		if (sequenceIndex == 0)
		{

		}
		if (sequenceIndex == this.Count)
		{
			enumerators.Add(getEnumerable(sequenceIndex).GetEnumerator());
		}
		else if (sequenceIndex == this.Count - 1)
		{
			//Correct to perform nothing
		}
		else
		{
			for (int i = sequenceIndex + 1; i < this.enumerators.Count; i++)
				this.enumerators[i].Dispose();
			this.enumerators.RemoveRange(sequenceIndex + 1);
		}

		Contract.Assert(this.Count == sequenceIndex + 1);
		return enumerators[sequenceIndex].MoveNext();
	}
	/// <summary> Gets the current element of the specified sequence.  </summary>
	/// <param name="sequenceIndex"> The index of the sequence to return the current element from. </param>
	public T Current(int sequenceIndex)
	{
		return enumerators[sequenceIndex].Current;
	}

	public void Dispose()
	{
		foreach (IEnumerator<T> enumerator in this.enumerators)
			enumerator.Dispose();
	}
	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	private string[] debuggerview
	{
		get
		{
			var result = new List<string>();
			foreach (var enumerator in this.enumerators)
			{
				if (enumerator == null)
					result.Add("Enumerator exhausted");
				else
				{
					T current;
					try
					{
						current = enumerator.Current;
					}
					catch (InvalidOperationException)
					{
						result.Add("Enumerator uninitialized");
						break;
					}
					result.Add(ReferenceEquals(null, current) ? "null" : current.ToString()!);
				}
			}
			return result.ToArray();
		}
	}
}
