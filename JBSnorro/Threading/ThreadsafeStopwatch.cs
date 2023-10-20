using System.Diagnostics;

namespace JBSnorro.Threading;

/// <summary>
/// <seealso href="https://stackoverflow.com/q/37799650/308451"/>
/// </summary>
public class ThreadsafeStopwatch
{
    // Stopwatch offset for last reset
    private long _lastResetTime;

    public static ThreadsafeStopwatch StartNew()
    {
        var result = new ThreadsafeStopwatch();
        return result;
    }
    private ThreadsafeStopwatch()
    {
        this.Reset();
    }

    /// Resets this instance.
    public void Reset()
    {
        // must keep in mind that GetTimestamp ticks are NOT DateTime ticks
        // (i.e. they must be divided by Stopwatch.Frequency to get seconds,
        // and Stopwatch.Frequency is hw dependent
        Interlocked.Exchange(ref _lastResetTime, Stopwatch.GetTimestamp());
    }

    /// Seconds elapsed since last reset
    public double ElapsedSeconds
    {
        get
        {
            var resetTime = Interlocked.Read(ref _lastResetTime);
            return (Stopwatch.GetTimestamp() - resetTime) / Stopwatch.Frequency;
        }
    }
    public long ElapsedMilliseconds
    {
        get
        {
            return (long)(ElapsedSeconds * 1000);
        }
    }
}
