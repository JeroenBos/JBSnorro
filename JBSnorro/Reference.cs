using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	public sealed class Reference<T>
	{
		public T Value { get; set; }
		public Reference()
			: this(default(T))
		{
		}
		public Reference(T value)
		{
			Value = value;
		}
	}
}
