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

        public BitArrayReadOnlySegment(BitArray data, ulong start, ulong length)
        {
            this.data = data;
            this.start = start;
            this.Length = length;
        }

        public bool this[int index] => this[(ulong)index];
        public bool this[ulong index] => this.data[start + index];

        public ulong Length { get; private set; }

        public IEnumerator<bool> GetEnumerator()
        {
            Contract.Assert<NotImplementedException>(start + this.Length <= int.MaxValue);
            return Enumerable.Range(0, ((IReadOnlyList<bool>)this).Count).Select(i => this[i]).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public BitReader ToBitReader(ulong startIndex = 0)
        {
            return new BitReader(data, startIndex, this.Length);
        }
        int IReadOnlyCollection<bool>.Count
        {
            get
            {
                Contract.Assert<NotImplementedException>(this.Length <= int.MaxValue);
                return (int)this.Length;
            }

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
