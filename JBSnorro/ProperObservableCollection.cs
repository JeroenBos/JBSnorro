using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary> An observable collection with methods allowing for range changes. </summary>
	public class ProperObservableCollection<T> : ObservableCollection<T>
	{
		/// <summary> Triggers the PropertyChanged event if the new value different from the current value. </summary>
		/// <param name="field"> A reference to the current value. </param>
		/// <param name="value"> The new value of the property. </param>
		/// <param name="propertyName"> The name of the property. </param>
		protected void Set<TProperty>(ref TProperty field, TProperty value, [CallerMemberName] string propertyName = null)
		{
			TProperty oldValue = field;
			if (!EqualityComparer<TProperty>.Default.Equals(oldValue, value))
			{
				field = value;
				var e = new PropertyMutatedEventArgs<TProperty>(propertyName, oldValue, value);
				base.OnPropertyChanged(e);
			}
		}
		public virtual void AddRange(IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				base.Add(item);
			}
		}
		public virtual void InsertRange(int index, IEnumerable<T> selections)
		{
			//TODO: properly implement observable collection range changes to invoke collection changed just once
			foreach (var selection in selections)
				base.Insert(index++, selection);
		}
		public virtual void RemoveRange(int index, int count)
		{
			for (int i = 0; i < count; i++)
			{
				this.RemoveAt(index);
			}
		}
		/// <summary> Replaces the entire contents of this collection with the specified items. </summary>
		/// <param name="newItems"></param>
		public virtual void Replace(IEnumerable<T> newItems)
		{
			Contract.Requires(newItems != null);

			Replace(0, this.Count, newItems);
		}
		public virtual void Replace(int startIndex, int count, IEnumerable<T> newItems)
		{
			foreach (var selection in newItems)
			{
				if (count-- > 0)
				{
					base[startIndex++] = selection;
				}
				else
				{
					Insert(startIndex++, selection);
				}
			}
		}
		public virtual void MoveRange(int oldStartIndex, int count, int newStartIndex)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Removes all elements in this collection that match the specified predicate.
		/// </summary>
		public void Remove(Func<T, bool> predicate)
		{
			//PERF: aggregate
			for (int i = 0; i < this.Count; i++)
			{
				if (predicate(this[i]))
				{
					this.RemoveAt(i);
					i--;
				}
			}
		}
	}
}
