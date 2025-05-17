using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using JBSnorro.Text.Json;
using JBSnorro.Diagnostics;

namespace Tests.JBSnorro;

[TestClass]
public class JsonSerializationTests
{
	[TestMethod]
	public void ObjectIsJsonIdempotent()
	{
		var obj = new object();
		var options = new JsonSerializerOptions();
		options.Converters.Add(new ExactPolymorphicJsonConverter<object>());
		string json = JsonSerializer.Serialize(obj, typeof(object));
		Contract.Assert(json == "{}");
		var deserializedObj = JsonSerializer.Deserialize<object>("{}");

		Contract.Assert(deserializedObj != null);
		// Contract.Assert(deserializedObj.GetType() == typeof(object)); // It's a Json token, because the code that creates a new object is in the ExactJsonConverter
	}


	[TestMethod]
	public void NumberIsSerializedAsNumber()
	{
		var options = new JsonSerializerOptions();
		options.Converters.Add(new DefaultObjectJsonConverter());
		string json = JsonSerializer.Serialize(1, typeof(int), options);

		Contract.Assert(json == "1");
	}

	[TestMethod]
	public void TabIsJsonIdempotent()
	{
		string tab = "\t";
		string serialized = "\"\\t\"";
		JsonSerializerOptions options = new JsonSerializerOptions();

		string json = JsonSerializer.Serialize(tab, typeof(string));
		Contract.Assert(json == serialized);

		var deserialized = JsonSerializer.Deserialize<string>(serialized);

		Contract.Assert(deserialized == tab);
	}

	class TestClass
	{
		public string S { get; } = "S";
	}
#if !NET5_0_OR_GREATER
	[TestMethod]
	public void TestWhyObjectForWhichNoConverterIsDefinedIsStillConverted()
	{
		var options = new JsonSerializerOptions();
		var converter = options.GetConverter(typeof(TestClass));

		Contract.Assert(converter == null);
		// ok there's no converter, but why DOES it convert?
		var serialized = JsonSerializer.Serialize(new TestClass());
		Contract.Assert(serialized == "{\"S\":\"S\"}");

		// lesson: apparently there's default JsonConverters in the JsonSerializer which are not in the JsonSerializerOptions.Converters collection...
	}
#endif // This got fixed
}

class ObjectWithOneProp
{
	public string A { get; } = "a";
}

    [TestClass]
    public class DefaultObjectJsonConverterTests
    {
        private static JsonSerializerOptions options
        {
            get
            {
                var result = new JsonSerializerOptions();
                result.Converters.Add(new DefaultObjectJsonConverter());
                return result;
            }
        }

        [TestMethod]
        public void ConvertNewObject()
        {
            string serialized = JsonSerializer.Serialize(new object(), options);
            Contract.Assert(serialized == "{}");
        }

        [TestMethod]
        public void ConvertObjectWithOneProperty()
        {
            string serialized = JsonSerializer.Serialize(new ObjectWithOneProp(), options);
            Contract.Assert(serialized == "{\"A\":\"a\"}");
        }


    }

    [TestClass]
public class ExtraPropertyJsonConverterTests
{
	private JsonSerializerOptions getOptions(JsonSerializerOptions? options = null, Func<object, object?>? getValue = null, string name = "name")
	{
		var result = new JsonSerializerOptions();
		result.Converters.Add(new ExtraPropertyJsonConverter(name, getValue ?? defaultGetValue, options));
		return result;

		object? defaultGetValue(object obj) => "VALUE";
	}

	[TestMethod]
	public void AddExtraPropertyToNewObject()
	{
		string serialized = JsonSerializer.Serialize(new object(), getOptions());
#if NET6_0_OR_GREATER
		Contract.Assert(serialized == "{\"name\":\"VALUE\"}");
#else
		// It's not the above because you can't override  how 'new object()' is serialized in .NET 5.0. 
		// Even though the converter.GetConvert method gets called. converter.Write(..) is not
		Contract.Assert(serialized == "{}");
#endif
	}

	[TestMethod]
	public void AddExtraPropertyToObjectWithOneProp()
	{
		string serialized = JsonSerializer.Serialize(new ObjectWithOneProp(), getOptions());
		Contract.Assert(serialized == "{\"name\":\"VALUE\",\"A\":\"a\"}");
	}

	[TestMethod]
	public void DoNotAddExtraPropertyToObjectWithOneProp()
	{
		string serialized = JsonSerializer.Serialize(new ObjectWithOneProp(), getOptions(getValue: obj => null));
		Contract.Assert(serialized == "{\"A\":\"a\"}");
	}
}
