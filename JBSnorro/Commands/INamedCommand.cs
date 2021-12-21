using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro.Commands
{
	public interface INamedCommand
	{
		string CommandName { get; }
	}
}
