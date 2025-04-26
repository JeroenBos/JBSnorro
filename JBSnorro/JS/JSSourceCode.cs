using JBSnorro.Diagnostics;
using System.Diagnostics;

namespace JBSnorro.JS;

[DebuggerDisplay("JSSourceCode({Value})")]
public class JSSourceCode
{
    public static JSSourceCode Null { get; } = new JSSourceCode("null");
    public static JSSourceCode Undefined { get; } = new JSSourceCode("undefined");

    public string Value { get; }
    public JSSourceCode(string sourceCode)
    {
        Contract.Requires(sourceCode != null, nameof(sourceCode));
        Value = sourceCode;
    }
}
