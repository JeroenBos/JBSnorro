using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JBSnorro.Csx
{
	public static class CsxExtensions
	{
		public static bool PathsRequireMnt = false;
		public static async Task<ProcessOutput> Execute(this string bash, bool stripStdoutTrailingNewline = true, CancellationToken cancellationToken = default)
		{
			var (exitCode, stdOut, stdErr) = await ProcessExtensions.ExecuteBashViaTempFile(bash, includeMnt: PathsRequireMnt, cancellationToken);

			// if (exitCode != 0)
			//	throw new BashNonzeroExitCodeException(exitCode);

			if (stripStdoutTrailingNewline)
			{
				stdOut = stdOut.TrimEnd('\n', '\r');
				stdErr = stdErr.TrimEnd('\n', '\r');
			}

			return (exitCode, stdOut, stdErr);
		}

		public static Task<ProcessOutput> Execute(this string command, string cwd, bool stripStdoutTrailingNewline = true, CancellationToken cancellationToken = default)
		{
			// translate Windows path to bash path
			cwd = ProcessExtensions.ToBashPath(cwd, includeMnt: PathsRequireMnt);

			return Execute($"export	GIT_CONFIG_SYSTEM=/dev/null; export GIT_CONFIG_GLOBAL=/dev/null; cd '{cwd}' && {command}", stripStdoutTrailingNewline, cancellationToken);
		}

	}

	public class BashError : Exception
	{
		public BashError() { }
		public BashError(string message) : base(message) { }
		public BashError(string message, Exception innerException) : base(message, innerException) { }
	}
	public class BashNonzeroExitCodeException : BashError
	{
		public BashNonzeroExitCodeException(int exitCode) : base($"Exited with code {exitCode}")
		{

		}
		public BashNonzeroExitCodeException(int exitCode, string stdErr) : base(stdErr)
		{

		}
	}
}
