using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskExtensions = JBSnorro.Extensions.TaskExtensions;

namespace Tests.JBSnorro.Extensions;

[TestClass]
public class FileExtensionsTests
{
    const int step_ms = 500;
    const int timeout_ms = 6 /* = #steps + 1 */ * step_ms;
    [TestMethod, Timeout(timeout_ms)]
    public async Task TestReadAllLinesContinuously()
    {
        // Arrange
        const int step_ms = 200;
        await using AsyncDisposable<string> pathContainer = IOExtensions.CreateTemporaryFile();
        var path = pathContainer.Value;

        var isDone = new Reference<bool>();
        var readLines = new List<string>();
        var readerTask = TaskExtensions.WithTimeout(reader, TimeSpan.FromMilliseconds(timeout_ms));
        var writerTask = TaskExtensions.WithTimeout(writer, TimeSpan.FromMilliseconds(timeout_ms));

        async Task writer(CancellationToken cancellationToken)
        {
            File.WriteAllLines(path, new string[] { "line 1" });
            await Task.Delay(2 * step_ms);
            File.AppendAllText(path, "partial line 2. ");
            await Task.Delay(4 * step_ms);
            File.AppendAllLines(path, new string[] { "end of line 2" });

        }


        // Act
        async Task reader(CancellationToken cancellationToken)
        {
            await foreach (var line in FileExtensions.ReadAllLinesContinuously(path, isDone, cancellationToken))
            {
                readLines.Add(line);
            }
        }


        // Assert
        await Task.Delay(1 * step_ms);
        Contract.AssertSequenceEqual(readLines, new string[] {
            "line 1",
        });

        await Task.Delay(3 * step_ms);
        Contract.AssertSequenceEqual(readLines, new string[] {
            "line 1",
        });

        await Task.Delay(5 * step_ms);
        Contract.AssertSequenceEqual(readLines, new string[] {
            "line 1",
            "partial line 2. end of line 2",
        });

        isDone.Value = true;
    }
}
