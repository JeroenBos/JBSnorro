using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	public interface IIndexable<out T> : IEnumerable<T>
	{
		T this[int index] { get; }
	}

	public interface ICountable<out T> : IEnumerable<T>
	{
		int Count { get; }
	}
}
