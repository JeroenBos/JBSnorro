using JBSnorro;
using JBSnorro.Csx;
using JBSnorro.Csx.Node;
using JBSnorro.Extensions;
using JBSnorro.IO;
using JBSnorro.Text;
using JBSnorro.Text.Json;
using System.Collections;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JBSnorro.JS;

public class JSProcessRunner : IJSRunner
{
    private readonly INodePathResolver nodePathResolver;
    private string nodePath => this.nodePathResolver.Path;
    public JSProcessRunner(INodePathResolver nodePathResolver)
    {
        this.nodePathResolver = nodePathResolver;
    }

    async Task<DebugProcessOutput> IJSRunner.ExecuteJSViaTempFile(string js)
    {
        string path = Path.GetTempFileName();
        await using var pathCleaner = TempFileCleanup.Register(path);
        await File.AppendAllTextAsync(path, js);
        var output = await new ProcessStartInfo(this.nodePath, $"\"{path}\"").WaitForExitAndReadOutputAsync();
        var result = ExtractDebugOutput(output);
        return result;
    }
    public Task<DebugProcessOutput> ExecuteJS(JSSourceCode sourceCode, IEnumerable<JSString> imports)
    {
        string js = JSBuilder.Build(sourceCode, imports);

        Global.AddDebugObject(js);

        return ExecuteJS(js);
    }
    public Task<DebugProcessOutput> ExecuteJS(JSSourceCode js)
    {
        return ExecuteJS(js.Value);
    }
    public async Task<DebugProcessOutput> ExecuteJS(string js)
    {
        string escapedJs = BashEscape(js);
        var output = await new ProcessStartInfo(this.nodePath, $"-e \"{escapedJs}\"").WaitForExitAndReadOutputAsync();

        var result = ExtractDebugOutput(output);

        Global.AddDebugObject(result.DebugOutput);
        return result;
    }
    private DebugProcessOutput ExtractDebugOutput(ProcessOutput output)
    {
        return (output.ExitCode,
                output.StandardOutput.SubstringAfter("__DEBUG__\n"),
                output.ErrorOutput,
                output.StandardOutput.SubstringUntil("__DEBUG__\n")
               );
    }

    /// <summary> Returns the bash-encoded js. </summary>
    internal static string BashEscape(string js)
    {
        var result = new List<char>();
        bool prevWasEscapingSlash = false;
        for (int i = 0; i < js.Length; i++)
        {
            if (prevWasEscapingSlash)
            {
                prevWasEscapingSlash = false;
                result.Add('\\');
                switch (js[i])
                {
                    case '"':
                        result.Add('\\');
                        break;
                    // slashes before these characters explicitly aren't escaped: (maybe add f, b, ... ?)
                    case 't':
                    case 'n':
                    case 'r':
                    case '\\':
                    default:
                        break;
                }
            }
            switch (js[i])
            {
                case '\\':
                    prevWasEscapingSlash = true;
                    break;
                case '"':
                    result.Add('\\');
                    result.Add('"');
                    break;

                default:
                    result.Add(js[i]);
                    break;
            }
        }
        if (prevWasEscapingSlash)
        {
            result.Add('\\');
        }
        return new string(result.ToArray());
    }
    /// <param name="imports"> Either paths or resolvable package names. </param>
    /// <param name="identifier"> A member to access. To treat a string as raw JS, wrap it in a JSString. </param>
    /// <param name="arguments"> 
    /// The arguments used in the method invocation (assuming the member access resolves to method). 
    /// Specify null if the member access is attribute access. 
    /// To treat a string argument as raw JS, wrap it in a JSString. 
    /// </param>
    public Task<DebugProcessOutput> ExecuteJS(
        IEnumerable<JSString> imports,
        object identifier,
        IReadOnlyList<object>? arguments = null,
        string? intermediateJS = null,
        JsonSerializerOptions? options = null)
    {
        string js = JSBuilder.Build(imports, identifier, arguments, intermediateJS, options);

        Global.AddDebugObject(js);

        return ExecuteJS(js);
    }


    /// <param name="jsIdentifiers"> The JS identifiers must be imported by imports. Order matters! </param>
    public Task<DebugProcessOutput> ExecuteJS(
        IEnumerable<JSString> imports,
        object identifier,
        IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
        IReadOnlyList<object>? arguments = null,
        JsonSerializerOptions? options = null,
        string typeIdPropertyName = "SERIALIZATION_TYPE_ID")
    {
        string js = JSBuilder.Build(imports, identifier, jsIdentifiers, arguments, options, typeIdPropertyName);
        Global.AddDebugObject(js);
        return ExecuteJS(js);
    }
}

internal class JSBuilder
{
    public static string Build(JSSourceCode sourceCode, IEnumerable<JSString> imports)
    {
        var jsBuilder = new ConfigurableStringBuilder();
        foreach (var import in imports)
            jsBuilder.AppendLine(ToJavascriptImportStatement(import));
        jsBuilder.AppendLine();
        jsBuilder.Append(sourceCode.Value);

        return jsBuilder.ToString();
    }

    internal static string Build(IEnumerable<JSString> imports,
                                 object identifier,
                                 IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
                                 IReadOnlyList<object>? arguments,
                                 JsonSerializerOptions? options,
                                 string typeIdPropertyName)
    {
        ArgumentNullException.ThrowIfNull(jsIdentifiers);

        foreach (var id in jsIdentifiers)
        {
            if (id.Key == null) throw new ArgumentException($"{nameof(jsIdentifiers)}.Key is null");
            if (string.IsNullOrEmpty(id.Value)) throw new ArgumentException($"{nameof(jsIdentifiers)}.Value is null or empty");
            if (id.Key.GetType() == typeof(string)) throw new ArgumentException($"{nameof(jsIdentifiers)} types cannot be string");
            if (id.Key.GetType().IsEnum) throw new ArgumentException($"{nameof(jsIdentifiers)} types cannot be enums");
            if (id.Key.GetType().IsPrimitive) throw new ArgumentException($"{nameof(jsIdentifiers)} types cannot be primitives");
            if (id.Key.GetType().IsInterface) throw new NotImplementedException($"{nameof(jsIdentifiers)} interfaces not implemented");
            if (id.Key.GetType().IsGenericParameter) throw new ArgumentException($"{nameof(jsIdentifiers)} types cannot be generic type parameters");
            if (id.Key.GetType().IsSignatureType) throw new ArgumentException($"{nameof(jsIdentifiers)} types cannot be delegate types");
        }

        options = CreateNewAndAssertValid(options);
        var extraPropOptions = CreateExtraPropertyJsonConverter(jsIdentifiers, options, typeIdPropertyName);


        string deserializeTypes = jsIdentifiers.Select(kvp => kvp.Value).Join(", ")!;
        string intermediateJs = @"
const FIELD_NAME = '" + typeIdPropertyName + @"'; // the serializable identifier of the type
const deserializableTypes = [" + deserializeTypes + @"]; // if the type has a `static deserialize(type, value)` method, that is used for deserialization


for (const type of deserializableTypes) {
	type[FIELD_NAME] = 1 + deserializableTypes.indexOf(type);
	if (type.deserialize === undefined) {
		type.deserialize = function (type, value) {
			const result = new type();
			for (let key in value) {
				if (key != FIELD_NAME) {
					result[key] = value[key];
				}
			}
			return result;
		}
	}
}
const getTypeFromDeserializationId = function (id) {
	for (const type of deserializableTypes) {
		if (!type.hasOwnProperty(FIELD_NAME))
			throw new Error(`Expected type to have static '${FIELD_NAME}'`);
		if (type[FIELD_NAME] == id) {
			return type;
		}
	}
	throw new Error(""Invalid deserialization id '"" + id + ""' (type="" + (typeof id) + "")"");
}
const reviver = function (key, value) {
	if (Array.isArray(value)) {
		for (let i = 0; i < value.length; i++) {
			value[i] = reviver(i, value[i]);
		}
	}
	else if (typeof value === 'object' && value !== null) {
		if (value.hasOwnProperty(FIELD_NAME)) {
			const typeId = value[FIELD_NAME];
			const type = getTypeFromDeserializationId(typeId);
			return type.deserialize(type, value);
		}
	}
	return value;
}";

        // pre-serialize the arguments, such that they can be wrapped with `JSON.parse(..., reviver)`:
        var serializedArguments = arguments?.Select(WrapInReviver).ToReadOnlyList<object>();
        var serializedIdentifier = WrapInReviver(identifier);

        JSSourceCode WrapInReviver(object arg)
        {
            if (arg is JSSourceCode s)
                return s;

            string serialized = Serialize(arg, extraPropOptions);
            bool isArray = serialized.StartsWith('[');
            if (isArray || IExtraPropertyJsonConverter.WillAddExtraProperty(arg, obj => getTypeIdentifierValue(jsIdentifiers, obj)))
            {
                string yesSerializeAgain = JsonSerializer.Serialize(serialized, options);
                return new JSSourceCode($"JSON.parse({yesSerializeAgain}, reviver)");
            }
            else
            {
                return new JSSourceCode(serialized);
            }
        }

        return Build(imports, serializedIdentifier, serializedArguments, intermediateJs, options);
    }

    public static string Build(IEnumerable<JSString> imports,
                                   object identifier,
                                   IReadOnlyList<object>? arguments = null,
                                   string? intermediateJS = null,
                                   JsonSerializerOptions? options = null)
    {
        options = CreateNewAndAssertValid(options);
        var jsBuilder = new ConfigurableStringBuilder();
        foreach (var import in imports)
            jsBuilder.AppendLine(ToJavascriptImportStatement(import));
        jsBuilder.AppendLine();
        if (intermediateJS != null)
        {
            jsBuilder.Append(intermediateJS);
            jsBuilder.AppendLine();
        }
        foreach (var (arg, i) in (arguments ?? Array.Empty<object>()).WithIndex())
        {
            string serialized = Serialize(arg, options);
            jsBuilder.AppendLine($"var arg{i} = {serialized};");
        }

        string identifier_js = Serialize(identifier, options);
        // add call-parentheses around args, or not call at all if args == null:
        string memberaccess = arguments == null ? "" : $"({Enumerable.Range(0, arguments.Count).Select(i => $"arg{i}").Join(", ")})";

        jsBuilder.AppendLine($"var result = {identifier_js}{memberaccess};");
        jsBuilder.AppendLine();
        jsBuilder.AppendLine("console.log('__DEBUG__');");
        jsBuilder.AppendLine("console.log(JSON.stringify(result));");

        string js = jsBuilder.ToString();
        return js;
    }

    internal static string ToJavascriptImportStatement(string pathOrPackageOrImportStatement)
    {
        if (pathOrPackageOrImportStatement.StartsWith("var ") || pathOrPackageOrImportStatement.StartsWith("const "))
            return pathOrPackageOrImportStatement;

        if (pathOrPackageOrImportStatement.Contains('\\') || pathOrPackageOrImportStatement.Contains('/') || pathOrPackageOrImportStatement.Contains('.'))
        {
            string packageName = Path.GetFileNameWithoutExtension(pathOrPackageOrImportStatement)
                                     .ToLower();
            // POSIX "Fully portable filenames" basically only contain these. Once we hit e.g. a '.' it's pr
            packageName = new string(packageName.TakeWhile(StringExtensions.IsLetterOrDigitOrUnderscore).ToArray());

            return $"const {packageName} = require('{pathOrPackageOrImportStatement}');";
        }
        else
        {
            string packageName = pathOrPackageOrImportStatement;
            return $"const {packageName} = require('{packageName}');";
        }
    }

    private static string Serialize(object arg, JsonSerializerOptions? options)
    {
        return arg switch
        {
            null => "undefined",
            JSSourceCode js => js.Value,
            string s when s.StartsWith('\'') => throw new ArgumentException("JSONs can't start with \"'\""),
            _ => JsonSerializer.Serialize(arg, arg.GetType().IsArray ? typeof(object[]) : arg.GetType(), options)
        };
    }

    private static JsonSerializerOptions CreateNewAndAssertValid(JsonSerializerOptions? options)
    {
        // the only character treated specially by UnsafeRelaxedJsonEscaping are \ and "
        // see https://github.com/dotnet/runtime/blob/cd8759d1bc94778f0bf35bc99dcdabf1b40cd71c/src/libraries/System.Text.Encodings.Web/src/System/Text/Encodings/Web/UnsafeRelaxedJavaScriptEncoder.cs
        options ??= new JsonSerializerOptions();
        options.Encoder ??= JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        string encoded = options.Encoder.Encode("\"");
        switch (encoded)
        {
            case "\\\"":
                break; // this is OK
            case "\\u0022":
                throw new ArgumentException("Invalid default encoder specified: A double quote in JSON would be encoded as '\\\\u0022', which is not valid javascript");
            default:
                throw new ArgumentException("Unknown invalid encoder specified. ");
        }
        encoded = options.Encoder.Encode("\\");
        switch (encoded)
        {
            case "\\\\":
                break; // this is OK
            default:
                throw new ArgumentException("Unknown invalid encoder specified for '\\'.");
        }
        return options;
    }


    internal static JsonSerializerOptions CreateExtraPropertyJsonConverter(
    IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
    JsonSerializerOptions? options,
    string typeIdPropertyName)
    {
        options = CreateNewAndAssertValid(options);

        var extraPropOptions = new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var converter = new ExtraPropertyJsonConverter(typeIdPropertyName, obj => getTypeIdentifierValue(jsIdentifiers, obj), options);
        extraPropOptions.Converters.Add(converter);
        extraPropOptions.Converters.Add(new IEnumerableJsonConverter<IEnumerable>());
        return extraPropOptions;
    }
    private static object? getTypeIdentifierValue(IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers, object? obj)
    {
        if (obj == null)
        {
            return null;
        }
        int typeIndex = jsIdentifiers.IndexOf(kvp => kvp.Key == obj.GetType());
        if (typeIndex == -1)
            return null;
        // 0 is skipped in JS.
        return 1 + typeIndex;
    }

}
