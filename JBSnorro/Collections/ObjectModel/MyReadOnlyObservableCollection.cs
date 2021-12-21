using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections.ObjectModel
{
	public interface IReadOnlyObservableCollection<out T> : IReadOnlyList<T>, INotifyCollectionChanged
	{
	}
	public class MyReadOnlyObservableCollection<T> : ReadOnlyObservableCollection<T>, IReadOnlyObservableCollection<T>
	{
		public new event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add { base.CollectionChanged += value; }
			remove { base.CollectionChanged -= value; }
		}

		public MyReadOnlyObservableCollection(ObservableCollection<T> list) : base(list)
		{

		}
		public MyReadOnlyObservableCollection(IEnumerable<T> enumerable) : this(new ObservableCollection<T>(enumerable))
		{
		}
	}
}
