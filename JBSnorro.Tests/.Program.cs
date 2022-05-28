using System.Diagnostics;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	class Program
	{
		// [DebuggerHidden] // If you do this debugging in VS is really slow
		public static Task Main(string[] args) => Testing.TestExtensions.DefaultMainTestProjectImplementation(args);
	}
}
