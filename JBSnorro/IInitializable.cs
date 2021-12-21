using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro
{
	public interface IInitializable
	{
		bool Initialized { get; }
		void Initialize();
	}
}
