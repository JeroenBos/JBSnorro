using System.Collections;
using System.Text.Json;
using JBSnorro.Diagnostics;
using JBSnorro.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro;

[TestClass]
public class JsonEnumerableSerializationTests
{
	private JsonSerializerOptions IEnumerableJsonSerializerOptions
	{
		get
		{
			var converter = new IEnumerableJsonConverter<IEnumerable>();
			var options = new JsonSerializerOptions();
			options.Converters.Add(converter);
			options.Converters.Add(new DefaultObjectJsonConverter());
			return options;
		}
	}
	[TestMethod]
	public void CanSerializeEmptyArray()
	{
		var serialized = JsonSerializer.Serialize(Array.Empty<int>(), IEnumerableJsonSerializerOptions);

		Contract.Assert(serialized == "[]");
	}
	[TestMethod]
	public void CanSerializeArrayWithInt()
	{
		var serialized = JsonSerializer.Serialize(new int[] { 0 }, IEnumerableJsonSerializerOptions);

		Contract.Assert(serialized == "[0]");
	}


	class TestObject
	{
#if NET6_0_OR_GREATER
		public string A { get; init; } = "a";
#else
		public string A { get; set; } = "a";
#endif
	}
	/// <summary> This test can do something the default System.Text.Json cannot. </summary>
	[TestMethod]
	public void CanSerializeArrayWithNestedObject()
	{
		var serialized = JsonSerializer.Serialize(new TestObject[] { new() }, IEnumerableJsonSerializerOptions);

		Contract.Assert(serialized == "[{\"A\":\"a\"}]");
	}

	[TestMethod]
	public void CanSerializeNestedArraysWithInts()
	{
		var serialized = JsonSerializer.Serialize(new int[][] { Array.Empty<int>(), new int[] { 0 } }, IEnumerableJsonSerializerOptions);

		Contract.Assert(serialized == "[[],[0]]");
	}
#if NET6_0_OR_GREATER
	[TestMethod]
	public void CanSerializeNestedArraysWithNestedObjects()
	{
		var serialized = JsonSerializer.Serialize(new TestObject[][] { Array.Empty<TestObject>(), new[] { new TestObject(), new TestObject() { A = "b" } } }, IEnumerableJsonSerializerOptions);

		Contract.Assert(serialized == "[[],[{\"A\":\"a\"},{\"A\":\"b\"}]]");
	}
#endif
	[TestMethod]
	public void CanSerializeString()
	{
		var serialized = JsonSerializer.Serialize("asdf", IEnumerableJsonSerializerOptions);

		Contract.Assert(serialized == "\"asdf\"");
	}

	[TestMethod]
	public void CanSerializeEmptyDictionary()
	{
		var serialized = JsonSerializer.Serialize(new Dictionary<string, int>(), IEnumerableJsonSerializerOptions);

		Contract.Assert(serialized == "{}");
	}
}
