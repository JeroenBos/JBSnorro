#nullable enable
using JBSnorro.Csx;
using JBSnorro.Csx.Node;
using JBSnorro.Text;
using System.Text.Json;

namespace JBSnorro.JS;

public interface IJSRunner
{
    static IJSRunner Create(INodePathResolver nodePathResolver) => new JSProcessRunner(nodePathResolver);

    Task<DebugProcessOutput> ExecuteJS(string js);
    public Task<DebugProcessOutput> ExecuteJS(
        IEnumerable<JSString> imports,
        object identifier,
        IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
        IReadOnlyList<object>? arguments = null,
        JsonSerializerOptions? options = null,
        string typeIdPropertyName = "SERIALIZATION_TYPE_ID");

    Task<DebugProcessOutput> ExecuteJS(JSSourceCode sourceCode, IEnumerable<JSString> imports);

    Task<DebugProcessOutput> ExecuteJS(JSSourceCode js);

    Task<DebugProcessOutput> ExecuteJS(
        IEnumerable<JSString> imports,
        object identifier,
        IReadOnlyList<object>? arguments = null,
        string? intermediateJS = null,
        JsonSerializerOptions? options = null);

    Task<DebugProcessOutput> ExecuteJSViaTempFile(string js);

    string ExecuteJS_Builder(IEnumerable<JSString> imports,
                                      object identifier,
                                      IReadOnlyList<object>? arguments = null,
                                      string? intermediateJS = null,
                                      JsonSerializerOptions? options = null);

    string ExecuteJS_Builder(IEnumerable<JSString> imports,
                                            object identifier,
                                            IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
                                            IReadOnlyList<object>? arguments,
                                            JsonSerializerOptions? options,
                                            string typeIdPropertyName);
}

