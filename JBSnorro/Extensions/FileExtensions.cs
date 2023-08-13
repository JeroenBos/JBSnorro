using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Text;

namespace JBSnorro.Extensions;

public static class FileExtensions
{
    /// <summary>
    /// Continuously reads all lines of a file while allowing other processes to still write to it.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="done">A boolean indicating whether we can stop reading all lines.</param>
    /// <param name="cancellationToken">A cancellation token for regular throw-on-canceled use.</param>
    public static async IAsyncEnumerable<string> ReadAllLinesContinuously(
        string path,
        Reference<bool>? done = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);


        StringBuilder? stringBuilder = null;
        while (true)
        {
            string? nextLine;
            try
            {
                nextLine = await sr.ReadLineAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                if (done?.Value == true)
                    break;
                else
                    throw;
            }
            if (nextLine != null)
            {
                sr.BaseStream.Position--;
                int ch = sr.Read();
                if (ch == '\n')
                {
                    if (stringBuilder == null)
                    {
                        yield return nextLine;
                    }
                    else
                    {
                        stringBuilder.Append(nextLine);
                        yield return stringBuilder.ToString();
                        stringBuilder = null;
                    }
                }
                else
                {
                    stringBuilder ??= new StringBuilder();
                    stringBuilder.Append(nextLine);
                }

            }
            else if (done?.Value == true)
            {
                break;
            }
            else
            {
                try
                {
                    await Task.Delay(10, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    if (done?.Value == true)
                        break;
                    else
                        throw;
                }
            }
        }

    }
}
