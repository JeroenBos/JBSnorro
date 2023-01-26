#nullable enable
using JBSnorro;
using JBSnorro.Csx;
using System.Diagnostics;
using System.Reflection;

namespace JBSnorro;

public static class ProcessExtensions
{
    // see https://stackoverflow.com/a/2374560/308451
    /// <summary> Gets whether the calling method is on the main thread. </summary>
    public static bool IsMainThread()
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA
         && !Thread.CurrentThread.IsBackground
         && !Thread.CurrentThread.IsThreadPoolThread
         && Thread.CurrentThread.IsAlive)
        {
            MethodInfo correctEntryMethod = Assembly.GetEntryAssembly()!.EntryPoint!;
            var trace = new System.Diagnostics.StackTrace();
            var frames = trace.GetFrames();
            for (int i = frames.Length - 1; i >= 0; i--)
            {
                MethodBase method = frames[i].GetMethod()!;
                if (correctEntryMethod == method)
                {
                    return true;
                }
            }
        }
        return false;
    }
    /// <summary>
    /// Waits asynchronously for the process to exit.
    /// </summary>
    /// <param name="process">The process to wait for cancellation.</param>
    /// <param name="cancellationToken">A cancellation token. If invoked, the task will return immediately as canceled.</param>
    /// <returns>A Task representing waiting for the process to end.</returns>
    public static Task<int> WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<int>();
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) => tcs.TrySetResult(process.ExitCode);
        if (cancellationToken != default)
            cancellationToken.Register(tcs.SetCanceled);

        return tcs.Task;
    }
    /// <summary>
    /// Normally when a processes is started from C#, it is a child process of the calling process. In the case the calling process exists, the child process is also terminated.
    /// This method starts a process without this relaton with the calling process, so that new process can outlive it.
    /// </summary>
    /// <returns>A Task representing waiting for the process to end.</returns>
    public static Task<int> StartIndependentlyAsync(string executable, params string[] arguments)
    {
        return startIndependentlyAsync(executable, visibly: true, arguments: arguments);
    }
    /// <summary>
    /// Normally when a processes is started from C#, it is a child process of the calling process. In the case the calling process exists, the child process is also terminated.
    /// This method starts a process without this relaton with the calling process, so that new process can outlive it, and starts it without showing the cmd.
    /// </summary>
    /// <returns>A Task representing waiting for the process to end.</returns>
    public static Task<int> StartIndependentlyInvisiblyAsync(string executable, params string[] arguments)
    {
        return startIndependentlyAsync(executable, visibly: false, arguments: arguments);
    }

    private static Task<int> startIndependentlyAsync(string executable, bool visibly, params string[] arguments)
    {
        if (string.IsNullOrEmpty(executable)) throw new ArgumentNullException(nameof(executable));
        if (arguments == null) throw new ArgumentNullException(nameof(arguments));
        if (arguments.Any(string.IsNullOrEmpty)) throw new ArgumentException(nameof(arguments));

        var info = new ProcessStartInfo(executable, string.Join(" ", arguments));
        if (!visibly)
        {
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
        }
        return info.WaitForExitAsync();
    }

    /// <summary>
    /// Normally when a processes is started from C#, it is a child process of the calling process. In the case the calling process exists, the child process is also terminated.
    /// This method starts a process without ths relaton with the calling process, so that new process can outlive it.
    /// </summary>
    /// <returns>A Task representing waiting for the process to end.</returns>
    public static async Task<int> WaitForExitAsync(this ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo)!;
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
    public static Task<ProcessOutput> WaitForExitAndReadOutputAsync(string executable, params string[] arguments)
    {
        return WaitForExitAndReadOutputAsync(executable, cancellationToken: default, arguments: arguments);
    }
    public static Task<ProcessOutput> WaitForExitAndReadOutputAsync(string executable, CancellationToken cancellationToken, params string[] arguments)
    {
        return new ProcessStartInfo(executable, string.Join(" ", arguments)).WaitForExitAndReadOutputAsync(cancellationToken);
    }
    public static async Task<ProcessOutput> WaitForExitAndReadOutputAsync(this ProcessStartInfo startInfo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Process? process = null;
        try
        {
            process = Process.Start(startInfo.WithOutput())!;

            cancellationToken.ThrowIfCancellationRequested();

            await process.WaitForExitAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            string output = process.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd();
            return new ProcessOutput { ExitCode = process.ExitCode, StandardOutput = output, ErrorOutput = errorOutput };
        }
        finally
        {
            process?.Kill(entireProcessTree: true);
        }
    }

    public static Task<int> StartInvisiblyAsync(this ProcessStartInfo startInfo)
    {
        return startInfo.WithHidden().WaitForExitAsync();
    }

    public static ProcessStartInfo WithHidden(this ProcessStartInfo startInfo)
    {
        startInfo.CreateNoWindow = true;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        return startInfo;
    }

    public static ProcessStartInfo WithOutput(this ProcessStartInfo startInfo)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        return startInfo;
    }

    public static Task<ProcessOutput> ExecuteBash(string bash, CancellationToken cancellationToken = default)
    {
        string encoded = bash.Replace("\"", "\\\""); // not sure if correct
        return new ProcessStartInfo("bash", $"-c \"{encoded}\"").WaitForExitAndReadOutputAsync(cancellationToken);

    }
    public static async Task<ProcessOutput> ExecuteBashViaTempFile(string bash, bool includeMnt = true, CancellationToken cancellationToken = default)
    {
        string path = Path.GetTempFileName() + ".exe"; // windows needs it to be 'executable'
        bash = bash.Replace("\r", "");
        if (!bash.StartsWith("#"))
        {
            bash = "#!/bin/bash\n" + bash;
        }
        await File.AppendAllTextAsync(path, bash, cancellationToken);


        string bashDir;
        ProcessStartInfo process;
        if (OperatingSystem.IsWindows())
        {
            const string bashExePath = "C:\\Program Files\\Git\\bin\\bash.exe"; // this used to be "C:\\Windows\\System32\\bash.exe" but that one suddenly stopped working (exit code 1, no output, so I assume failed windows update)
            string redirecterFile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.dotnet/execute.sh";

            string bashPath = ToBashPath(path, includeMnt: includeMnt);
            string bashFile = Path.GetFileName(bashPath).Replace("\\", "/");
            bashDir = Path.GetDirectoryName(bashPath)!.Replace("\\", "/");

            string bashRedirecterFile = ToBashPath(redirecterFile, includeMnt: false);
            if (!File.Exists(redirecterFile))
            {
                Directory.CreateDirectory("~/.dotnet");
                File.WriteAllLines(redirecterFile, new[]
                {
                "#!/bin/bash",
                "cd \"$1\"",
                "pwd",
                "./\"$2\"",
                });
            }

            string args = $"'{bashRedirecterFile}' '{bashDir}' '{bashFile}'";
            process = new ProcessStartInfo(bashExePath, $"-c \"{args}\"");
        }
        else
        {
            bashDir = Path.GetDirectoryName(path)!;
            process = new ProcessStartInfo("/bin/bash", path);
        }

        var result = await process.WaitForExitAndReadOutputAsync(cancellationToken);
        if (result.StandardOutput.StartsWith(bashDir + "\n"))
        {
            result = result.With(standardOutput: result.StandardOutput[(bashDir.Length + "\n".Length)..]);
        }
        return result;
    }
    public static string ToBashPath(this string path, bool includeMnt = true)
    {
        if (path.Length >= 2 && path[1] == ':')
        {
            path = $"/{char.ToLower(path[0])}{path[2..]}";
            if (includeMnt)
            {
                path = "/mnt" + path;
            }
        }
        path = path.Replace('\\', '/');
        return path;
    }
    public static string ToWindowsPath(this string path, bool alsoOnOtherOSes = false)
    {
        if (!alsoOnOtherOSes && !OperatingSystem.IsWindows())
            return path;

        if (path.StartsWith('/'))
        {
            if (path.Length > 3 && path[2] == '/')
            {
                path = path[1] + ":" + path[2..];
            }
        }
        else if (path.StartsWith('~'))
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
        }
        else if (path.StartsWith("%UserProfile%", StringComparison.OrdinalIgnoreCase))
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path["%UserProfile%".Length..];
        }
        return path.Replace("/", "\\");
    }

}