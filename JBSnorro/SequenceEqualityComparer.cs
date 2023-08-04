using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
    public sealed class SequenceEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        public static readonly SequenceEqualityComparer<T> InOrderComparer = new SequenceEqualityComparer<T>(Enumerable.SequenceEqual);
        public static readonly SequenceEqualityComparer<T> AnyOrderComparer = new SequenceEqualityComparer<T>((x, y) => EnumerableExtensions.ContainsSameElements(x, y));
        /// <summary>
        /// Creates a comparer that compares its arguments whether they contain the same elements, in the same order, as determined per the specified (element) equality comparer.
        /// </summary>
        public static SequenceEqualityComparer<T> CreateInOrderComparer(IEqualityComparer<T>? equalityComparer = null)
        {
            Contract.Requires(equalityComparer != null || EqualityComparer<T>.Default != null);

            return new SequenceEqualityComparer<T>((first, second) => Enumerable.SequenceEqual(first, second, equalityComparer ?? EqualityComparer<T>.Default));
        }

        private readonly Func<IEnumerable<T>, IEnumerable<T>, bool> equalityComparer;
        private SequenceEqualityComparer(Func<IEnumerable<T>, IEnumerable<T>, bool> equalityComparer)
        {
            this.equalityComparer = equalityComparer;
        }
        public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            return equalityComparer(x, y!);
        }

        public static int GetHashCode(IEnumerable<T> obj)
        {
            unchecked
            {
                int result = 1;
                foreach (T element in obj)
                {
                    if (element is not null)
                    {
                        result += element.GetHashCode() * 17;
                    }
                }
                return result;
            }
        }
        int IEqualityComparer<IEnumerable<T>>.GetHashCode(IEnumerable<T> sequence) => GetHashCode(sequence);

    }
}
