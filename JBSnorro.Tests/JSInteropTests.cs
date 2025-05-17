#nullable enable
using JBSnorro;
using JBSnorro.Csx.Node;
using JBSnorro.Extensions;
using JBSnorro.JS;
using JBSnorro.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using static JBSnorro.Diagnostics.Contract;

namespace Tests.JBSnorro.JS;

public class JSTestsBase
{
    protected static readonly IEnumerable<JSString> NO_IMPORTS = Array.Empty<JSString>();
    protected static string? NodePath => EnvironmentExtensions.GetRequiredEnvironmentVariable("NODE_PATH");
    protected readonly INodePathResolver nodePathResolver;
    protected readonly IJSRunner jsRunner;
    public JSTestsBase()
    {
        if (NodePath is null)
            this.nodePathResolver = INodePathResolver.FromCommand(); // through DI maybe?
        else
            this.nodePathResolver = INodePathResolver.FromPath(NodePath);
        this.jsRunner = IJSRunner.Create(this.nodePathResolver);
    }
}
[TestClass]
public class JSInteropTests : JSTestsBase
{
    [TestMethod]
    public async Task CanResolveNodeExecutable()
    {
        var process = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, "--version").WaitForExitAndReadOutputAsync();

        Assert(process.ExitCode == 0);
        Assert(new Regex("[0-9][0-9]\\..*").IsMatch(process.StandardOutput));
    }

    private async Task<string> executeJS(string js)
    {
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(js);
        Console.WriteLine(stderr);
        Console.WriteLine(debugOutput);
        return stdout;
    }

    private async Task<string> executeJS(object arg, JsonSerializerOptions? options = null)
    {
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(NO_IMPORTS, arg, options: options);
        Console.WriteLine(stderr);
        Console.WriteLine(debugOutput);
        return stdout;
    }

    [TestMethod]
    public async Task Simple()
    {
        string result = await executeJS("console.log('hi')");
        Assert(result == "hi\n");
    }


    [TestMethod]
    public async Task DoubleQuotes()
    {
        string result = await executeJS("console.log(\"hi\")");
        Assert(result == "hi\n");
    }


    [TestMethod]
    public async Task EmptyArray()
    {
        string result = await executeJS("console.log(\"hi\")");
        Assert(result == "hi\n");
    }


    [TestMethod]
    public async Task StringArg()
    {
        string result = await executeJS((object)"hi");
        Assert(result == "\"hi\"\n");
    }

    [TestMethod]
    public async Task DeserializeOutputString()
    {
        string output = await executeJS((object)"hi");
        string? result = JsonSerializer.Deserialize<string>(output);
        Assert(result == "hi");
    }

    [TestMethod]
    public async Task DeserializeOutputNumber()
    {
        string output = await executeJS(5);
        var result = JsonSerializer.Deserialize<int>(output);
        Assert(result == 5);
    }

    [TestMethod]
    public async Task DeserializeOutputObject()
    {
        string output = await executeJS(new TestObject { Number = 10 });
        var result = JsonSerializer.Deserialize<TestObject>(output);
        Assert(result?.Number == 10);
    }
    class TestObject
    {
        public int Number { get; set; }
    }


    [TestMethod]
    public async Task EmptyString()
    {
        const string input = "\"\"";
        string jsonEncoded = await executeJS((object)input);
        Assert(jsonEncoded == "\"\\\"\\\"\"\n"); // this result makes sense because it is the json encoded of "\"\""
        Assert(JsonSerializer.Deserialize<string>(jsonEncoded) == input);
    }
    [TestMethod]
    public async Task StringC()
    {
        // literal JS:
        // console.log("\"c\"");
        const string input = "\"c\"";
        string jsonEncoded = await executeJS((object)input);
        Assert(jsonEncoded == "\"\\\"c\\\"\"\n");  // this result makes sense because it is the json encoded of "\"c\""
        Assert(JsonSerializer.Deserialize<string>(jsonEncoded) == input);
    }

    [TestMethod]
    public async Task StringTab()
    {
        const string deserializesToTab = "\"\\t\"";
        void defaultJavascriptEncoderTabDeserialization()
        {
            Assert(JsonSerializer.Deserialize<string>(deserializesToTab) == "\t");
        }
        void unsafeJavascriptEncoderTabDeserialization()
        {
            var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            Assert(JsonSerializer.Deserialize<string>(deserializesToTab, options) == "\t");
        }

        // this is correct js because it's tested in preTest via temp file rather than via bash
        // this is what the JS should be such that a tab is printed:
        const string correct_js = "\nvar result = \"\\t\";\n\nconsole.log('__DEBUG__');\nconsole.log(JSON.stringify(result));\n";
        async Task preTest()
        {
            var (_, stdOut, _, _) = await this.jsRunner.ExecuteJSViaTempFile(correct_js);
            Assert(stdOut == deserializesToTab + "\n");
        }

        defaultJavascriptEncoderTabDeserialization();
        unsafeJavascriptEncoderTabDeserialization();
        await preTest();


        // literal JS:
        // console.log("\t");
        const string input = "\t";

        // test the built JS:
        string js = JSBuilder.Build(NO_IMPORTS, input);
        Assert(correct_js == js);

        // test the built JS, escaping to bash and executing: 
        string jsOutput = await executeJS((object)input);
        Assert(jsOutput == deserializesToTab + "\n"); // if this fails it must be the bashEscape
    }
    [TestMethod]
    public async Task ExecutingJSViaTempFileOrBashIsIdentical()
    {
        foreach (string s in new[] {
            "\\\\sqrt{}",  // literal js: console.log("\\sqrt{}")
				"\\\\b",  // literal js: console.log("\\b")
				"\\\"",        // literal js: console.log("\"")
				"\\\"\\\"",    // literal js: console.log("\"\"")
				"\\t",         // literal js: console.log("\t")
				"\t",          // literal js: console.log("	")
				"\\n" })       // literal js: console.log("\n")
        {
            string js = $"console.log(\"{s}\")";
            var (_, stdOutViaFile, fileErr, _) = await this.jsRunner.ExecuteJSViaTempFile(js);
            var (_, stdOutViaBash, bashErr, _) = await this.jsRunner.ExecuteJS(js);


            Assert(fileErr == "");
            Assert(bashErr == "");
            Assert(stdOutViaFile == stdOutViaBash);
        }
    }
    [TestMethod]
    public void OptionsUnderstanding()
    {
        var options = new JsonSerializerOptions();
        var result = JsonSerializer.Serialize("\"\"", options);
        Assert(result == "\"\\u0022\\u0022\"");

        var options2 = new JsonSerializerOptions();
        options2.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        var result2 = JsonSerializer.Serialize("\"\"", options2);
        Assert(result2 == "\"\\\"\\\"\"");

        Assert(JsonSerializer.Deserialize<string>(result, options) == "\"\"");
        Assert(JsonSerializer.Deserialize<string>(result2, options2) == "\"\"");

        var result3 = JsonSerializer.Serialize(new TestString(), options);
        Assert(result3 == "{\"A\":\"\\u0022\"}");

        var result4 = JsonSerializer.Serialize(new TestString(), options2);
        Assert(result4 == "{\"A\":\"\\\"\"}");
    }
    class TestString
    {
        public string A { get; } = "\"";
    }
    [TestMethod]
    public async Task EscapingUnderstanding()
    {
        {
            string arg = new string(Array.Empty<char>());
            var a = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(a.StandardOutput == "\n");
            Assert(JSBuilder.Build(NO_IMPORTS, "", new object[] { a.StandardOutput }).Contains(arg));
        }
        {
            string arg = new string(new char[] { '\'', '\'' });
            var b = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(b.StandardOutput == "\n");
            // Assert(JSBuilder.Build(new string[0], "", new object[] { b.StandardOutput }).Contains(arg));
        }
        {
            string arg = new string(new char[] { '"', '"', '"', '"' });
            var b2 = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(b2.StandardOutput == "\n");
            // Assert(JSBuilder.Build(new string[0], "", new object[] { b2.StandardOutput }).Contains(arg));
        }
        {
            string arg = new string(new char[] { '"', 'a', '"' });
            var c = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(c.ErrorOutput.Contains("ReferenceError: a is not defined"));
        }
        {
            // literal JS:
            // console.log("a");
            string arg = new string(new char[] { '\\', '"', 'a', '\\', '"' });
            var d = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(d.StandardOutput == "a\n");
            string js = JSBuilder.Build(NO_IMPORTS, "", new object[] { d.StandardOutput[..^1] });
            Assert(js.Contains(arg.Replace("\\\\", "\\").Replace("\\\"", "\"")));
        }
        {
            // literal JS:
            // console.log("\"");
            string arg = new string(new char[] { '\\', '"', '\\', '\\', '\\', '"', '\\', '"' });
            var e = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(e.StandardOutput == "\"\n");
            string js = JSBuilder.Build(NO_IMPORTS, "", new object[] { e.StandardOutput[..^1] });
            Assert(js.Contains(arg.Replace("\\\\", "\\").Replace("\\\"", "\"")));
        }
        {
            // literal JS:
            // console.log("\\");
            string arg = new string(new char[] { '\\', '"', '\\', '\\', '\\', '\\', '\\', '"' });
            var f = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(f.StandardOutput == "\\\n");
            string js = JSBuilder.Build(NO_IMPORTS, "", new object[] { f.StandardOutput[..^1] });
            Assert(js.Contains(arg.Replace("\\\\", "\\").Replace("\\\"", "\"")));
        }
        {
            // literal JS:
            // console.log("\"hi");
            string arg = new string(new char[] { '\\', '"', '\\', '\\', '\\', '"', 'h', 'i', '\\', '"' });
            var g = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(g.StandardOutput == "\"hi\n");
            string js = JSBuilder.Build(NO_IMPORTS, "", new object[] { g.StandardOutput[..^1] });
            Assert(js.Contains(arg.Replace("\\\\", "\\").Replace("\\\"", "\"")));
        }
        {
            // literal JS:
            // console.log("\"\"");
            string arg = new string(new char[] { '\\', '"', '\\', '\\', '\\', '"', '\\', '\\', '\\', '"', '\\', '"' });
            var h = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(h.StandardOutput == "\"\"\n");
            string js = JSBuilder.Build(NO_IMPORTS, "", new object[] { h.StandardOutput[..^1] });
            Assert(js.Contains(arg.Replace("\\\\", "\\").Replace("\\\"", "\"")));
        }
        {
            // literal JS:
            // console.log("\\");
            string arg = new string(new char[] { '\\', '"', '\\', '\\', '\\', '\\', '\\', '"' });
            var i = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(i.StandardOutput == "\\\n");
            string js = JSBuilder.Build(NO_IMPORTS, "", new object[] { i.StandardOutput[..^1] });
            Assert(js.Contains(arg.Replace("\\\\", "\\").Replace("\\\"", "\"")));
        }
        {
            // literal JS:
            // console.log("\\\\");
            string arg = new string(new char[] { '\\', '"', '\\', '\\', '\\', '\\', '\\', '\\', '\\', '\\', '\\', '"' });
            var j = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
            Assert(j.StandardOutput == "\\\\\n");
            string js = JSBuilder.Build(NO_IMPORTS, "", new object[] { j.StandardOutput[..^1] });
            Assert(js.Contains(arg.Replace("\\\\", "\\").Replace("\\\"", "\"")));
        }
    }
    [TestMethod]
    public async Task TestTabRoundtrip()
    {
        // literal JS:
        // console.log("\t");
        string arg = new string(new char[] { '\\', '"', '\\', 't', '\\', '"' });
        var t = await new System.Diagnostics.ProcessStartInfo(this.nodePathResolver.Path, $"-e \"console.log({arg});\"").WaitForExitAndReadOutputAsync();
        Assert(t.StandardOutput == "\t\n");
        string js = JSBuilder.Build(identifier: t.StandardOutput[..^1], imports: NO_IMPORTS);
        Assert(js.Contains(arg.Replace("\\\\", "\\").Replace("\\\"", "\"")));
    }
    [TestMethod]
    public async Task TestTabViaFileIsSameAsViaDashE()
    {
        const string input = "console.log('\\t')";
        var (_, stdout, _, _) = await this.jsRunner.ExecuteJSViaTempFile(input);
        Assert(stdout == "\t\n");

        (_, stdout, _, _) = await this.jsRunner.ExecuteJS(input);
        Assert(stdout == "\t\n");
    }

    [TestMethod]
    public void TestTabIsntEscaped()
    {
        var options = new JsonSerializerOptions();
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        string serialized = JsonSerializer.Serialize("\t", options);
        Assert(serialized == "\"\\t\"");
        string? deserialized = JsonSerializer.Deserialize<string>(serialized, options);
        Assert(deserialized == "\t");
    }
}
[TestClass]
public class JSSerializationIdTests : JSTestsBase
{
    [TestMethod]
    public async Task PlainJSRuns()
    {
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(
            imports: NO_IMPORTS,
            identifier: "\"\"",
            jsIdentifiers: Array.Empty<KeyValuePair<Type, string>>(),
            arguments: null);
        Console.WriteLine(stderr);
        Console.WriteLine(debugOutput);
        Assert(stderr == "");
    }
    class TestObject
    {
        public string A { get; } = "A";
        public static KeyValuePair<Type, string> Identifier => new KeyValuePair<Type, string>(typeof(TestObject), nameof(TestObject));
    }
    [TestMethod]
    public async Task DeserializationTypeIdsAreSet()
    {
        string field_name = "FFF";
        var fakeImport = new JSString("var X; class TestObject { A = 'b' }");
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(
            imports: new[] { fakeImport },
            identifier: new JSSourceCode("TestObject." + field_name),
            jsIdentifiers: new[] { TestObject.Identifier },
            arguments: null,
            typeIdPropertyName: field_name
            );
        Assert(stderr == "");
        Assert(stdout == "1\n");
    }
    [TestMethod]
    public void SerializesWithTypeId()
    {
        var options = JSBuilder.CreateExtraPropertyJsonConverter(
            jsIdentifiers: new[] { TestObject.Identifier },
            options: null,
            typeIdPropertyName: "FFF"
        );

        string serialized = JsonSerializer.Serialize(new TestObject(), options);
        Assert(serialized == "{\"FFF\":1,\"A\":\"A\"}");
    }

    [TestMethod]
    public async Task JSDoesNotSerializeStaticAttributes()
    {
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(
            @"
				class TestObject
				{
					static A = 'A';
					B = 'B';
				}
				console.log(JSON.stringify(new TestObject()));
				");
        Assert(stderr == "");
        Assert(stdout == "{\"B\":\"B\"}\n");
    }
    [TestMethod]
    public async Task JSDoesNotSerializePrototypeAttributes()
    {
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(
            @"
				class TestObject
				{
					B = 'B';
				}
				TestObject.prototype['A'] = 'A';
				console.log(JSON.stringify(new TestObject()));
				");
        Assert(stderr == "");
        Assert(stdout == "{\"B\":\"B\"}\n");
    }

    [TestMethod]
    public void TestThatReviverIsCorrectlyInserted()
    {
        var typeIdPropertyName = "FFF";
        var fakeImport = new JSString("var X; class TestObject { A = 'b' }; const f = function(a) { return a.constructor.name; }");
        var js = JSBuilder.Build(
            imports: new[] { fakeImport },
            identifier: new JSSourceCode("f"),
            jsIdentifiers: new[] { TestObject.Identifier },
            arguments: new object[] { new TestObject() },
            options: null,
            typeIdPropertyName: typeIdPropertyName
        );


        Assert(!js.Contains("JSON.parse(f"));
        Assert(js.Contains("var arg0 = JSON.parse(\"{\\\"FFF\\\":1,\\\"A\\\":\\\"A\\\"}\", reviver);"));
        Assert(js.Contains("var result = f(arg0);"));
    }
    [TestMethod]
    public void TestThatReviverIsCorrectlyInsertedForArray()
    {
        var typeIdPropertyName = "FFF";
        var fakeImport = new JSString("var X; class TestObject { A = 'b' }; const f = function(a) { return a.constructor.name; }");
        var js = JSBuilder.Build(
            imports: new[] { fakeImport },
            identifier: new JSSourceCode("f"),
            jsIdentifiers: new[] { TestObject.Identifier },
            arguments: new object[] { new[] { new TestObject() } },
            options: null,
            typeIdPropertyName: typeIdPropertyName
        );

        Console.WriteLine(js);
        Assert(js.Contains("var arg0 = JSON.parse(\"[{\\\"FFF\\\":1,\\\"A\\\":\\\"A\\\"}]\", reviver);"));
    }

    [TestMethod]
    public async Task TryGetStaticMethodOnJSType()
    {
        string js =
        @"class TestObject { A = 'b' }
			const f = function(t) { t.myfunction = function() { console.log('ok'); } };
			f(TestObject)
			TestObject.myfunction()
			";
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(js);

        Assert(stderr == "");
        Assert(stdout == "ok\n");

    }
    [TestMethod]
    public async Task TestDirectDeserialization()
    {
        var typeIdPropertyName = "FFF";
        var fakeImport = new JSString("var X; class TestObject { A = 'b' }; const f = function(a) { return null; }");
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(
            imports: new[] { fakeImport },
            identifier: new JSSourceCode("TestObject.deserialize(TestObject, {\"FFF\":\"TestObject\"});"),
            jsIdentifiers: new[] { TestObject.Identifier },
            arguments: null,
            typeIdPropertyName: typeIdPropertyName
            );
        Console.WriteLine(stdout);
        Console.WriteLine(stderr);
        Console.WriteLine(debugOutput);
        Assert(stderr == "");
        Assert(stdout == "{\"A\":\"b\"}\n");
    }
    [TestMethod]
    public async Task IsDeserializedUsingTypeId()
    {
        var typeIdPropertyName = "FFF";
        var fakeImport = new JSString("var X; class TestObject { A = 'b' }; const f = function(a) { return a; }");
        var (exitcode, stdout, stderr, debugOutput) = await this.jsRunner.ExecuteJS(
            imports: new[] { fakeImport },
            identifier: new JSSourceCode("f"),
            jsIdentifiers: new[] { TestObject.Identifier },
            arguments: new object[] { new TestObject() },
            typeIdPropertyName: typeIdPropertyName
        );
        Console.WriteLine(stdout);
        Console.WriteLine(stderr);
        Console.WriteLine(debugOutput);
        Assert(stderr == "");
        Assert(stdout == "{\"A\":\"A\"}\n");
    }
}
