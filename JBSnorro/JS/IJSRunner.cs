using JBSnorro.Csx;
using JBSnorro.Csx.Node;
using JBSnorro.Text;
using System.Text.Json;

namespace JBSnorro.JS;

public interface IJSRunner
{
    static IJSRunner Create(INodePathResolver nodePathResolver) => new JSProcessRunner(nodePathResolver);

    /// <summary> Executes the specified JS and aggregates various outputs. </summary>
    Task<DebugProcessOutput> ExecuteJS(string js);
    /// <summary> Executes the specified JS and aggregates various outputs. </summary>
    Task<DebugProcessOutput> ExecuteJS(JSSourceCode js);
    Task<DebugProcessOutput> ExecuteJS(JSSourceCode sourceCode, IEnumerable<JSString> imports);
    /// <summary> Serializes the JS <paramref name="identifier"/> and aggregates various outputs. </summary>
    /// <param name="imports"> Either paths or resolvable package names. </param>
    /// <param name="identifier"> A member to access. To treat a string as raw JS, wrap it in a JSString. </param>
    /// <param name="arguments"> 
    /// The arguments used in the method invocation (assuming the member access resolves to method). 
    /// Specify null if the member access is attribute access. 
    /// To treat a string argument as raw JS, wrap it in a JSString. 
    /// </param>
    /// <param name="serializeTypeName"> Whether to include the field __type__ on each non-primitive with the type name when serializing a JS object. </param>
    Task<DebugProcessOutput> ExecuteJS(IEnumerable<JSString> imports,
                                       object identifier,
                                       IReadOnlyList<object>? arguments = null,
                                       string? intermediateJS = null,
                                       JsonSerializerOptions? options = null,
                                       bool serializeTypeName = false);
    /// <summary> Serializes the JS <paramref name="identifier"/> and aggregates various outputs. </summary>
    /// <param name="imports"> Either paths or resolvable package names. </param>
    /// <param name="identifier"> A member to access. To treat a string as raw JS, wrap it in a JSString. </param>
    /// <param name="arguments"> 
    /// The arguments used in the method invocation (assuming the member access resolves to method). 
    /// Specify null if the member access is attribute access. 
    /// To treat a string argument as raw JS, wrap it in a JSString. 
    /// </param>
    /// <param name="jsIdentifiers"> The JS identifiers must be imported by imports. Order matters! </param>
    /// <param name="serializeTypeName"> Whether to include the field __type__ on each non-primitive with the type name when serializing a JS object. </param>
    Task<DebugProcessOutput> ExecuteJS(IEnumerable<JSString> imports,
                                       object identifier,
                                       IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
                                       IReadOnlyList<object>? arguments = null,
                                       JsonSerializerOptions? options = null,
                                       string typeIdPropertyName = "SERIALIZATION_TYPE_ID",
                                       bool serializeTypeName = false);

    Task<DebugProcessOutput> ExecuteJSViaTempFile(string js);
}

