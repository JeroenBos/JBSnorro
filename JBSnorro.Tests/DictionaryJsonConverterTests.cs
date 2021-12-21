using JBSnorro;
using JBSnorro.Collections.ObjectModel;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JBSnorro.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JBSnorro.Tests
{
	[TestClass]
	public class DictionaryJsonConverterTests
	{
		JsonSerializerOptions options
		{
			get
			{
				var options = new JsonSerializerOptions();
				options.Converters.Add(new DictionaryJsonConverter<object, ReadOnlyDictionary<string, object>>(_ => new ReadOnlyDictionary<string, object>(_)));
				return options;
			}
		}

		JsonSerializerOptions optionsWithTest
		{
			get
			{
				var options = new JsonSerializerOptions();
				options.Converters.Add(new DictionaryJsonConverter<object, ReadOnlyDictionary<string, object>>(_ => new ReadOnlyDictionary<string, object>(_), elementTypes: new Dictionary<string, Type> { { "x", typeof(ITest) } }));
				options.Converters.Add(JBSnorro.Text.Json.ExactJsonConverter<ITest, Test>.Instance);
				return options;
			}
		}
		[TestMethod]
		public void ElementIsEmptyString()
		{
			var options = new JsonSerializerOptions();
			options.Converters.Add(new DictionaryJsonConverter<object, ReadOnlyDictionary<string, object>>(_ => new ReadOnlyDictionary<string, object>(_)));

			var result = JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": \"\"}", options);
			var s = result?.GetValueOrDefault("x", null) as string;
			Contract.Assert(s == "");
		}
		[TestMethod]
		public void ElementIsNumber()
		{
			var result = JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": 0}", options);
			var i = result?.GetValueOrDefault("x", null) as float?;
			Contract.Assert(i == 0);
		}
		[TestMethod]
		public void ElementIsEmptyArray()
		{
			var result = JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": []}", options);
			var a = result?.GetValueOrDefault("x", null);
			Contract.Assert((a as object[])?.Length == 0);
		}
		[TestMethod]
		public void ElementIsEmptyObject()
		{
			var result = JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": {}}", options);
			Contract.Assert(result.GetValueOrDefault("x", null) != null);
		}
		[TestMethod]
		public void ElementIsObjectRemainsJsonElementIfUndeserializable()
		{
			var result = JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": { \"a\": \"\" }}", options);
			Contract.Assert(result != null);
			Contract.Assert(result["x"] is JsonElement);
		}
		[TestMethod]
		public void ElementIsObject()
		{
			var options = optionsWithTest;
			var result = JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": { \"a\": \"\" }}", options);
			object test = result?.GetValueOrDefault("x", null);
			Contract.Assert(test is Test);
		}

		[TestMethod]
		public void ElementIsObjectWithTwoFields()
		{
			var result = JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": { \"a\": \"\", \"b\": \"\" }}", options);
			Contract.Assert(result != null);
			Contract.Assert(result["x"] is JsonElement);
		}

		[TestMethod]
		public void TwoElements()
		{
			var options = optionsWithTest;
			var result = JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": { \"a\": \"\" }, \"y\": { \"a\": \"\" }}", options);
			Contract.Assert(result != null);
			Contract.Assert(result["x"] is Test);
			Contract.Assert(result["y"] is JsonElement);
		}

		[TestMethod, ExpectedException(typeof(JsonException))]
		public void ElementIsObjectWithIncompatibleField()
		{
			var options = optionsWithTest;

			JsonSerializer.Deserialize<ReadOnlyDictionary<string, object>>("{\"x\": { \"a\": \"\", \"b\": \"\" }}", options);
		}

		interface ITest { string a { get; } }
		class Test : ITest
		{
			public string a { get; set; }
		}
	}
}
