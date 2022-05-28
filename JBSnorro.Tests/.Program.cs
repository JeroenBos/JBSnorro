using System.Diagnostics;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	class Program
	{
		// [DebuggerHidden] // Debugging in VS is slow if you do this
		public static Task Main(string[] args) => Testing.TestExtensions.DefaultMainTestProjectImplementation(args);
	}
}
