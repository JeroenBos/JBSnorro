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
    public void Just_check_no_exception_is_thrown()
    {
        var allGeneratedNumbers = new HashSet<int>();
        var generatedGenerators = RandomExtensions.GenerateRandomGenerators(0).Take(10);

        foreach (Random generator in generatedGenerators)
        {
            for (int i = 0; i < 10; i++)
            {
                allGeneratedNumbers.Add(generator.Next(0 ,int.MaxValue));
            }
        }

        Contract.Assert(allGeneratedNumbers.Count > 90);
    }
    [TestMethod]
    public void Same_results_should_be_yielded_on_repeated_querying_with_same_seed()
    {
        var sequence1 = RandomExtensions.GenerateSerializableRandomGenerators(0);
        var drawnNumbers1 = sequence1.First().NextArray(10, 0, int.MaxValue);

        var sequence2 = RandomExtensions.GenerateSerializableRandomGenerators(0);
        var drawnNumbers2 = sequence2.First().NextArray(10, 0, int.MaxValue);

        Contract.AssertSequenceEqual(drawnNumbers1, drawnNumbers2);
    }
    [TestMethod]
    public void Same_results_should_be_yielded_on_deserialization()
    {
        var sequence = RandomExtensions.GenerateSerializableRandomGenerators(0);

        var generator = sequence.Skip(10).First();
        var expected = generator.NextArray(10, 0, int.MaxValue);


        var deserializedSequence = new RandomExtensions.SerializableRandomGenerator(sequence.Seed, sequence.CurrentIndex);
        var actual = deserializedSequence.First().NextArray(10, 0, int.MaxValue);

        Contract.AssertSequenceEqual(actual, expected);
    }
}