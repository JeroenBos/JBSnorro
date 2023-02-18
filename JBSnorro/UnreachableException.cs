#if !NET7_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro
{
	/// <summary>
	/// An exception which should never be thrown, but states that the code shouldn't be reachable.
	/// </summary>
	public class UnreachableException : Exception
	{
		public UnreachableException()
			: base("This code should have been unreachable")
		{

		}
	}
}
#endif