using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
    public class BitArrayReadOnlySegment : IReadOnlyList<bool>
    {
        private readonly BitArray data;
        private readonly ulong start;
        private readonly ulong length;

        public BitArrayReadOnlySegment(BitArray data, ulong start, ulong length)
        {
            this.data = data;
            this.start = start;
            this.length = length;
        }

        public bool this[int index] => this[(ulong)index];
        public bool this[ulong index] => this.data[start + index];

        public int Count => (int)this.length;

        public IEnumerator<bool> GetEnumerator()
        {
            Contract.Assert<NotImplementedException>(start + length <= int.MaxValue);
            return Enumerable.Range(0, this.Count).Select(i => this[i]).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class BitArraySegmentExtensions
    {
        public static BitArrayReadOnlySegment SelectSegment(this BitArray array, Range range)
        {
            Contract.Assert<NotImplementedException>(array.Length <= int.MaxValue);
            var (index, length) = range.GetOffsetAndLength((int)array.Length);
            return array.SelectSegment(index, length);
        }
        public static BitArrayReadOnlySegment SelectSegment(this BitArray array, int start, int length)
        {
            return array.SelectSegment((ulong)start, (ulong)length);
        }
        public static BitArrayReadOnlySegment SelectSegment(this BitArray array, ulong start, ulong length)
        {
            return new BitArrayReadOnlySegment(array, start, length);
        }
    }
}
