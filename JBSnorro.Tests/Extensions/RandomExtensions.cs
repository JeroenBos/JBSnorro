using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Tests.JBSnorro.Extensions;

[TestClass]
public class RandomExtensionsTests
{
    private JsonSerializerOptions SerializerOptions
    {
        get
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(RandomExtensions.JsonConverter);
            return options;
        }
    }
    [TestMethod]
    public void TestSerialization()
    {
        var random = new Random(1);


        var json = JsonSerializer.Serialize(random, SerializerOptions);
        var expected = DrawArray(random);


        var deserialized = JsonSerializer.Deserialize<Random>(json, SerializerOptions)!;
        var actual = DrawArray(deserialized);

        Contract.AssertSequenceEqual(expected, actual);
    }

    private int[] DrawArray(Random random)
    {
        var result = new int[10];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = random.Next();
        }
        return result;
    }
}

[TestClass]
public class TestRandomGeneratorDrawer
{
    [TestMethod]
    public static void Just_check_no_exception_is_thrown()
    {
        var allGeneratedNumbers = new HashSet<int>();
        var generatedGenerators = RandomExtensions.GenerateRandomGenerators(0).Take(10);

        foreach (var generator in generatedGenerators)
        {
            for (int i = 0; i < 10; i++)
            {
                allGeneratedNumbers.Add(generator.Next());
            }
        }

        Contract.Assert(allGeneratedNumbers.Count > 90);
    }
}