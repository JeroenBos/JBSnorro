#nullable enable
using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JBSnorro.Text;
using JBSnorro.Text.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JBSnorro.Csx;

namespace JBSnorro
{
    public static class ProcessExtensions
    {
        // see https://stackoverflow.com/a/2374560/308451
        /// <summary> Gets whether the calling method is on the main thread. </summary>
        public static bool IsMainThread()
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA
             && !Thread.CurrentThread.IsBackground
             && !Thread.CurrentThread.IsThreadPoolThread
             && Thread.CurrentThread.IsAlive)
            {
                MethodInfo correctEntryMethod = Assembly.GetEntryAssembly()!.EntryPoint!;
                var trace = new System.Diagnostics.StackTrace();
                var frames = trace.GetFrames();
                for (int i = frames.Length - 1; i >= 0; i--)
                {
                    MethodBase method = frames[i].GetMethod()!;
                    if (correctEntryMethod == method)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="process">The process to wait for cancellation.</param>
        /// <param name="cancellationToken">A cancellation token. If invoked, the task will return immediately as canceled.</param>
        /// <returns>A Task representing waiting for the process to end.</returns>
        public static Task<int> WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(process.ExitCode);
            if (cancellationToken != default)
                cancellationToken.Register(tcs.SetCanceled);

            return tcs.Task;
        }
        /// <summary>
        /// Normally when a processes is started from C#, it is a child process of the calling process. In the case the calling process exists, the child process is also terminated.
        /// This method starts a process without this relaton with the calling process, so that new process can outlive it.
        /// </summary>
        /// <returns>A Task representing waiting for the process to end.</returns>
        public static Task<int> StartIndependentlyAsync(string executable, params string[] arguments)
        {
            return startIndependentlyAsync(executable, visibly: true, arguments: arguments);
        }
        /// <summary>
        /// Normally when a processes is started from C#, it is a child process of the calling process. In the case the calling process exists, the child process is also terminated.
        /// This method starts a process without this relaton with the calling process, so that new process can outlive it, and starts it without showing the cmd.
        /// </summary>
        /// <returns>A Task representing waiting for the process to end.</returns>
        public static Task<int> StartIndependentlyInvisiblyAsync(string executable, params string[] arguments)
        {
            return startIndependentlyAsync(executable, visibly: false, arguments: arguments);
        }

        private static Task<int> startIndependentlyAsync(string executable, bool visibly, params string[] arguments)
        {
            if (string.IsNullOrEmpty(executable)) throw new ArgumentNullException(nameof(executable));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            if (arguments.Any(string.IsNullOrEmpty)) throw new ArgumentException(nameof(arguments));

            var info = new ProcessStartInfo(executable, string.Join(" ", arguments));
            if (!visibly)
            {
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
            }
            return info.WaitForExitAsync();
        }

        /// <summary>
        /// Normally when a processes is started from C#, it is a child process of the calling process. In the case the calling process exists, the child process is also terminated.
        /// This method starts a process without ths relaton with the calling process, so that new process can outlive it.
        /// </summary>
        /// <returns>A Task representing waiting for the process to end.</returns>
        public static async Task<int> WaitForExitAsync(this ProcessStartInfo startInfo)
        {
            var process = Process.Start(startInfo)!;
            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        public static Task<ProcessOutput> WaitForExitAndReadOutputAsync(string executable, params string[] arguments)
        {
            return WaitForExitAndReadOutputAsync(executable, cancellationToken: default, arguments: arguments);
        }
        public static Task<ProcessOutput> WaitForExitAndReadOutputAsync(string executable, CancellationToken cancellationToken, params string[] arguments)
        {
            return new ProcessStartInfo(executable, string.Join(" ", arguments)).WaitForExitAndReadOutputAsync(cancellationToken);
        }
        public static async Task<ProcessOutput> WaitForExitAndReadOutputAsync(this ProcessStartInfo startInfo, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var process = Process.Start(startInfo.WithOutput())!;

            cancellationToken.ThrowIfCancellationRequested();

            await process.WaitForExitAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            string output = process.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd();
            return new ProcessOutput { ExitCode = process.ExitCode, StandardOutput = output, ErrorOutput = errorOutput };
        }

        public static Task<int> StartInvisiblyAsync(this ProcessStartInfo startInfo)
        {
            return startInfo.WithHidden().WaitForExitAsync();
        }

        public static ProcessStartInfo WithHidden(this ProcessStartInfo startInfo)
        {
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            return startInfo;
        }

        public static ProcessStartInfo WithOutput(this ProcessStartInfo startInfo)
        {
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            return startInfo;
        }

        public static Task<ProcessOutput> ExecuteBash(string bash, CancellationToken cancellationToken = default)
        {
            string encoded = bash.Replace("\"", "\\\""); // not sure if correct
            return new ProcessStartInfo("bash", $"-c \"{encoded}\"").WaitForExitAndReadOutputAsync(cancellationToken);

        }
        public static async Task<ProcessOutput> ExecuteBashViaTempFile(string bash, bool includeMnt = true, CancellationToken cancellationToken = default)
        {
            string path = Path.GetTempFileName() + ".exe"; // windows needs it to be 'executable'
            bash = bash.Replace("\r", "");
            if (!bash.StartsWith("#"))
            {
                bash = "#!/bin/bash\n" + bash;
            }
            await File.AppendAllTextAsync(path, bash, cancellationToken);


            string bashDir;
            ProcessStartInfo process;
            if (OperatingSystem.IsWindows())
            {
                const string bashExePath = "C:\\Program Files\\Git\\bin\\bash.exe"; // this used to be "C:\\Windows\\System32\\bash.exe" but that one suddenly stopped working (exit code 1, no output, so I assume failed windows update)
                const string redirecterFile = "~/.dotnet/execute.sh";

                string bashPath = ToBashPath(path, includeMnt: includeMnt);
                string bashFile = Path.GetFileName(bashPath).Replace("\\", "/");
                bashDir = Path.GetDirectoryName(bashPath)!.Replace("\\", "/");

                string bashRedirecterFile = ToBashPath(redirecterFile, includeMnt: false);
                if (!File.Exists(redirecterFile))
                {
                    Directory.CreateDirectory("~/.dotnet");
                    File.WriteAllLines(redirecterFile, new[]
                    {
                    "#!/bin/bash",
                    "cd \"$1\"",
                    "pwd",
                    "./\"$2\"",
                    });
                }

                string args = $"'{bashRedirecterFile}' '{bashDir}' '{bashFile}'";
                process = new ProcessStartInfo(bashExePath, $"-c \"{args}\"");
            }
            else
            {
                bashDir = Path.GetDirectoryName(path)!;
                process = new ProcessStartInfo("/bin/bash", path);
            }

            var result = await process.WaitForExitAndReadOutputAsync(cancellationToken);
            if (result.StandardOutput.StartsWith(bashDir + "\n"))
            {
                result = result.With(standardOutput: result.StandardOutput[(bashDir.Length + "\n".Length)..]);
            }
            return result;
        }
        public static string ToBashPath(this string path, bool includeMnt = true)
        {
            if (path.Length >= 2 && path[1] == ':')
            {
                path = $"/{char.ToLower(path[0])}{path[2..]}";
                if (includeMnt)
                {
                    path = "/mnt" + path;
                }
            }
            path = path.Replace('\\', '/');
            return path;
        }
        public static string ToWindowsPath(this string path, bool alsoOnOtherOSes = false)
        {
            if (!alsoOnOtherOSes && !OperatingSystem.IsWindows())
                return path;

            if (path.StartsWith('/'))
            {
                if (path.Length > 3 && path[2] == '/')
                {
                    path = path[1] + ":" + path[2..];
                }
            }
            else if (path.StartsWith('~'))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
            }
            else if (path.StartsWith("%UserProfile%", StringComparison.OrdinalIgnoreCase))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path["%UserProfile%".Length..];
            }
            return path.Replace("/", "\\");
        }

        internal static async Task<DebugProcessOutput> ExecuteJSViaTempFile(string js)
        {
            string path = Path.GetTempFileName();
            await File.AppendAllTextAsync(path, js);
            var output = await new ProcessStartInfo("node", $"\"{path}\"").WaitForExitAndReadOutputAsync();
            var result = ExtractDebugOutput(output);
            return result;
        }
        public static Task<DebugProcessOutput> ExecuteJS(JSSourceCode sourceCode, IEnumerable<JSString> imports)
        {
            var jsBuilder = new ConfigurableStringBuilder();
            foreach (var import in imports)
                jsBuilder.AppendLine(ToJavascriptImportStatement(import));
            jsBuilder.AppendLine();
            jsBuilder.Append(sourceCode.Value);

            string js = jsBuilder.ToString();
            Global.AddDebugObject(js);
            return ExecuteJS(js);
        }
        public static Task<DebugProcessOutput> ExecuteJS(JSSourceCode js)
        {
            return ExecuteJS(js.Value);
        }
        public static async Task<DebugProcessOutput> ExecuteJS(string js)
        {
            string escapedJs = BashEscape(js);
            var output = await new ProcessStartInfo("node", $"-e \"{escapedJs}\"").WaitForExitAndReadOutputAsync();

            var result = ExtractDebugOutput(output);

            Global.AddDebugObject(result.DebugOutput);
            return result;
        }
        private static DebugProcessOutput ExtractDebugOutput(ProcessOutput output)
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
        public static Task<DebugProcessOutput> ExecuteJS(
            IEnumerable<JSString> imports,
            object identifier,
            IReadOnlyList<object>? arguments = null,
            string? intermediateJS = null,
            JsonSerializerOptions? options = null)
        {
            string js = ExecuteJS_Builder(imports, identifier, arguments, intermediateJS, options);
            Global.AddDebugObject(js);

            return ExecuteJS(js);
        }
        internal static JsonSerializerOptions CreateNewAndAssertValid(JsonSerializerOptions? options)
        {
            // the only character treated specially by UnsafeRelaxedJsonEscaping are \ and "
            // see https://github.com/dotnet/runtime/blob/cd8759d1bc94778f0bf35bc99dcdabf1b40cd71c/src/libraries/System.Text.Encodings.Web/src/System/Text/Encodings/Web/UnsafeRelaxedJavaScriptEncoder.cs
            options ??= new JsonSerializerOptions();
            if (options.Encoder == null)
            {
                options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            }
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
        // internal for testing
        internal static string ExecuteJS_Builder(IEnumerable<JSString> imports,
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
                string serialized = serialize(arg, options);
                jsBuilder.AppendLine($"var arg{i} = {serialized};");
            }

            string identifier_js = serialize(identifier, options);
            // add call-parentheses around args, or not call at all if args == null:
            string memberaccess = arguments == null ? "" : $"({Enumerable.Range(0, arguments.Count).Select(i => $"arg{i}").Join(", ")})";

            jsBuilder.AppendLine($"var result = {identifier_js}{memberaccess};");
            jsBuilder.AppendLine();
            jsBuilder.AppendLine("console.log('__DEBUG__');");
            jsBuilder.AppendLine("console.log(JSON.stringify(result));");

            string js = jsBuilder.ToString();
            return js;
        }
        private static string serialize(object arg, JsonSerializerOptions? options)
        {
            if (arg == null)
                return "undefined";
            if (arg is JSSourceCode js)
                return js.Value;
            if (arg is string s && s.StartsWith("'"))
                throw new ArgumentException("JSONs can't start with \"'\"");
            Type type = arg.GetType();
            // HACK: If the argument is an array, maybe it should an array of type `object[]` instead of `T[]` for some T
            if (type.IsArray)
                type = typeof(object[]);

            return JsonSerializer.Serialize(arg, type, options);

        }

        internal static JsonSerializerOptions CreateExtraPropertyJsonConverter(
            IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
            JsonSerializerOptions? options,
            string typeIdPropertyName)
        {
            options = CreateNewAndAssertValid(options);

            var extraPropOptions = new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var converter = new ExtraPropertyJsonConverter(typeIdPropertyName, obj => jsIdentifiers.getTypeIdentifierValue(obj), options);
            extraPropOptions.Converters.Add(converter);
#if NET6_0_OR_GREATER
            extraPropOptions.Converters.Add(new IEnumerableJsonConverter<IEnumerable>());
#endif
            return extraPropOptions;
        }
        private static object? getTypeIdentifierValue(this IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers, object? obj)
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
        /// <param name="jsIdentifiers"> The JS identifiers must be imported by imports. Order matters! </param>
        public static Task<DebugProcessOutput> ExecuteJS(
            IEnumerable<JSString> imports,
            object identifier,
            IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
            IReadOnlyList<object>? arguments = null,
            JsonSerializerOptions? options = null,
            string typeIdPropertyName = "SERIALIZATION_TYPE_ID")
        {
            string js = ExecuteJS_Builder(imports, identifier, jsIdentifiers, arguments, options, typeIdPropertyName);
            Global.AddDebugObject(js);
            return ExecuteJS(js);
        }

        internal static string ExecuteJS_Builder(IEnumerable<JSString> imports,
                                                 object identifier,
                                                 IReadOnlyList<KeyValuePair<Type, string>> jsIdentifiers,
                                                 IReadOnlyList<object>? arguments,
                                                 JsonSerializerOptions? options,
                                                 string typeIdPropertyName)
        {
            if (jsIdentifiers == null)
                throw new ArgumentNullException(nameof(jsIdentifiers));
            foreach (var id in jsIdentifiers)
            {
                if (id.Key == null) throw new ArgumentException(nameof(jsIdentifiers) + ".Key is null");
                if (string.IsNullOrEmpty(id.Value)) throw new ArgumentException(nameof(jsIdentifiers) + ".Value is null or empty");
                if (id.Key.GetType() == typeof(string)) throw new ArgumentException(nameof(jsIdentifiers) + " types cannot be string");
                if (id.Key.GetType().IsEnum) throw new ArgumentException(nameof(jsIdentifiers) + " types cannot be enums");
                if (id.Key.GetType().IsPrimitive) throw new ArgumentException(nameof(jsIdentifiers) + " types cannot be primitives");
                if (id.Key.GetType().IsInterface) throw new NotImplementedException(nameof(jsIdentifiers) + " interfaces not implemented");
                if (id.Key.GetType().IsGenericParameter) throw new ArgumentException(nameof(jsIdentifiers) + " types cannot be generic type parameters");
                if (id.Key.GetType().IsSignatureType) throw new ArgumentException(nameof(jsIdentifiers) + " types cannot be delegate types");
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

                string serialized = serialize(arg, extraPropOptions);
                bool isArray = serialized.StartsWith("[");
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

            return ExecuteJS_Builder(imports, serializedIdentifier, serializedArguments, intermediateJs, options);
        }



        interface IIdentifiableType
        {
            string JSIdentifier { get; }
            Type CSharpType { get; }
        }

        private static string ToJavascriptImportStatement(JSString pathOrPackageOrImportStatement) => ToJavascriptImportStatement(pathOrPackageOrImportStatement.Value);
        private static string ToJavascriptImportStatement(string pathOrPackageOrImportStatement)
        {
            if (pathOrPackageOrImportStatement.StartsWith("var "))
                return pathOrPackageOrImportStatement;

            if (pathOrPackageOrImportStatement.Contains("\\") || pathOrPackageOrImportStatement.Contains("//") || pathOrPackageOrImportStatement.Contains("."))
            {
                string packageName = Path.GetFileNameWithoutExtension(pathOrPackageOrImportStatement)
                                         .ToLower();
                // POSIX "Fully portable filenames" basically only contain these:
                packageName = new string(packageName.Where(StringExtensions.IsLetterOrDigitOrUnderscore).ToArray());

                return $"var {packageName} = require('{pathOrPackageOrImportStatement}');";
            }
            else
            {
                string packageName = pathOrPackageOrImportStatement;
                return $"var {packageName} = require('{packageName}');";
            }
        }

        class StringConverter : JsonConverter<string>
        {
            private static JsonEncodedText backtick = JsonEncodedText.Encode("`", JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
            private static JsonEncodedText slash = JsonEncodedText.Encode("\\", JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                if (options.Encoder == null) throw new ArgumentNullException("options.Encoder");
                // if feels very dumb to have to create an intermediate builder, but I can't seem to write directly to the underlying stream of the writer
                var builder = new StringWriter();

                builder.Write(backtick);
                int lastBacktickIndex = -1;
                ReadOnlySpan<char> _value = value.AsSpan();
                while (true)
                {
                    int nextBacktickIndex = value.IndexOf('`', lastBacktickIndex + 1);
                    if (nextBacktickIndex == -1)
                        break;
                    options.Encoder.Encode(builder, value, lastBacktickIndex, nextBacktickIndex - lastBacktickIndex);
                    builder.Write(slash);
                    lastBacktickIndex = nextBacktickIndex;
                }
                if (lastBacktickIndex == -1)
                    lastBacktickIndex = 0;
                options.Encoder.Encode(builder, value, lastBacktickIndex, value.Length - lastBacktickIndex);
                builder.Write(backtick);
                writer.WriteStringValue(builder.ToString());
            }

            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
        }
    }
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
}
