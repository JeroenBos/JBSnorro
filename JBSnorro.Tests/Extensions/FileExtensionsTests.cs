#pragma warning disable CS0168 // Variable is declared but never used

using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JBSnorro.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using TaskExtensions = JBSnorro.Extensions.TaskExtensions;

namespace Tests.JBSnorro.Extensions;

[TestClass]
public class FileExtensionsTests
{
    const int step_ms = 500;
    const int timeout_ms = 10 * step_ms + 5000 /* because in CI it's rather slow */;
    [TestMethod]
    public async Task TestReadAllLinesContinuouslyAThousandTimes()
    {
        for (int i = 0; i < 1000; i++)
        {
            Console.Write(".");
            try
            {
                await TestReadAllLinesContinuously();
            }
            catch (Exception ex)
            {
                int lineCount = 0;
                foreach (var writing in FileExtensions.GetWritings())
                {
                    lineCount++;
                    Console.Write(writing);
                }
                throw;
            }
            finally
            {
                FileExtensions.ClearWritings();
            }
        }
    }
    public async Task TestReadAllLinesContinuously()
    {
        // Arrange
        AsyncDisposable<string> pathContainer = IOExtensions.CreateTemporaryFile();
        var path = pathContainer.Value;
        Contract.Requires(File.Exists(path));

        var isDone = new Reference<bool>();
        var readLines = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        var readerTask = TaskExtensions.WithTimeout(reader, TimeSpan.FromMilliseconds(timeout_ms));
        var writerTask = TaskExtensions.WithTimeout(writer, TimeSpan.FromMilliseconds(timeout_ms));

        async Task writer(CancellationToken cancellationToken)
        {
            await Task.Delay(1 * step_ms);
            File.WriteAllLines(path, new string[] { "line 1" });
            FileExtensions.WriteLine($"{stopwatch.ElapsedMilliseconds} Written line 1");
            await Task.Delay(2 * step_ms);
            File.AppendAllText(path, "partial line 2. ");
            FileExtensions.WriteLine($"{stopwatch.ElapsedMilliseconds} Written partial line 2");
            await Task.Delay(4 * step_ms);
            File.AppendAllLines(path, new string[] { "end of line 2" });
            FileExtensions.WriteLine($"{stopwatch.ElapsedMilliseconds} Written line 2");
        }


        // Act
        async Task reader(CancellationToken cancellationToken)
        {
            await foreach (var line in FileExtensions.ReadAllLinesContinuously(path, isDone, cancellationToken))
            {
                FileExtensions.WriteLine($"{stopwatch.ElapsedMilliseconds} read line");
                readLines.Add(line);
            }
        }


        // Assert
        await Task.Delay(2 * step_ms);
        FileExtensions.WriteLine($"{stopwatch.ElapsedMilliseconds} Assertion 1");
        Assert.IsTrue(readLines.SequenceEqual(new string[] {
            "line 1",
        }));

        await Task.Delay(3 * step_ms);
        FileExtensions.WriteLine($"{stopwatch.ElapsedMilliseconds} Assertion 2");
        Assert.IsTrue(readLines.SequenceEqual(new string[] {
            "line 1",
        }));

        await Task.Delay(5 * step_ms);
        FileExtensions.WriteLine($"{stopwatch.ElapsedMilliseconds} Assertion 3");
        Assert.IsTrue(readLines.SequenceEqual(new string[] {
            "line 1",
            "partial line 2. end of line 2",
        }));

        isDone.Value = true;

    }
}
