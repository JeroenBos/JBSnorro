using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JBSnorro.Csx
{
    public record ProcessOutput
    {
        public int ExitCode { get; init; }
        public string StandardOutput { get; init; } = default!;
        public string ErrorOutput { get; init; } = default!;
        public void Deconstruct(out int exitCode, out string standardOutput, out string errorOutput)
        {
            exitCode = ExitCode;
            standardOutput = StandardOutput;
            errorOutput = ErrorOutput;
        }

        public static implicit operator ProcessOutput((int ExitCode, string StandardOutput, string StandardError) tuple)
        {
            return new ProcessOutput()
            {
                ExitCode = tuple.ExitCode,
                StandardOutput = tuple.StandardOutput,
                ErrorOutput = tuple.StandardError,
            };
        }
        [DebuggerHidden]
        public virtual ProcessOutput With(string standardOutput)
        {
            return new ProcessOutput()
            {
                StandardOutput = standardOutput,
                ErrorOutput = this.ErrorOutput,
                ExitCode = this.ExitCode,
            };
        }
    }

    public record DebugProcessOutput : ProcessOutput
    {
        public string DebugOutput { get; init; }

        public void Deconstruct(out int exitCode, out string standardOutput, out string errorOutput, out string debugOutput)
        {
            base.Deconstruct(out exitCode, out standardOutput, out errorOutput);
            debugOutput = DebugOutput;
        }

        public static implicit operator DebugProcessOutput((int ExitCode, string StandardOutput, string StandardError, string DebugOutput) tuple)
        {
            return new DebugProcessOutput()
            {
                ExitCode = tuple.ExitCode,
                StandardOutput = tuple.StandardOutput,
                ErrorOutput = tuple.StandardError,
                DebugOutput = tuple.DebugOutput,
            };
        }
        [DebuggerHidden]
        public override ProcessOutput With(string standardOutput)
        {
            return new DebugProcessOutput()
            {
                StandardOutput = standardOutput,
                ErrorOutput = this.ErrorOutput,
                ExitCode = this.ExitCode,
                DebugOutput = this.DebugOutput,
            };
        }
    }
}
