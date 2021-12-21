using JBSnorro.Diagnostics;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections.Sorted
{
	/// <summary> Represents a sorted indexable collection with known count. </summary>
	public interface ISortedList<T> : IIndexable<T>, ICountable<T>, ISortedEnumerable<T>
	{
	}
}
