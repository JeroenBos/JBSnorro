#nullable enable
using JBSnorro;
using JBSnorro.Csx;
using JBSnorro.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

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
        await using var pathCleanup = TempFileCleanup.Register(path);
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


    /// <summary>
    /// Find out what process(es) have a lock on the specified file.
    /// </summary>
    /// <param name="path">Path of the file.</param>
    /// <returns>Processes locking the file</returns>
    /// <remarks>See also:
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
    /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)
    /// 
    /// </remarks>
    public static List<Process> GetLockingProcesses(string path)
    {
        return FileUtil.GetLockingProcesses(path);
    }

    /// <summary>
    /// Copied from https://stackoverflow.com/a/20623311/308451.
    /// </summary>
    private static class FileUtil
    {
        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        const int RmRebootReasonNone = 0;
        const int CCH_RM_MAX_APP_NAME = 255;
        const int CCH_RM_MAX_SVC_NAME = 63;

        enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle,
                                              UInt32 nFiles,
                                              string[] rgsFilenames,
                                              UInt32 nApplications,
                                              [In] RM_UNIQUE_PROCESS[]? rgApplications,
                                              UInt32 nServices,
                                              string[]? rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        static extern int RmGetList(uint dwSessionHandle,
                                    out uint pnProcInfoNeeded,
                                    ref uint pnProcInfo,
                                    [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
                                    ref uint lpdwRebootReasons);

        internal static List<Process> GetLockingProcesses(string path)
        {
            uint handle;
            string key = Guid.NewGuid().ToString();
            List<Process> processes = new List<Process>();

            int res = RmStartSession(out handle, 0, key);
            if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const int ERROR_MORE_DATA = 234;
                uint pnProcInfoNeeded = 0,
                     pnProcInfo = 0,
                     lpdwRebootReasons = RmRebootReasonNone;

                string[] resources = new string[] { path }; // Just checking on one resource.

                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (res != 0) throw new Exception("Could not register resource.");

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == ERROR_MORE_DATA)
                {
                    // Create an array to store the process results
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    // Get the list
                    res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (res == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);

                        // Enumerate all of the results and add them to the 
                        // list to be returned
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            // catch the error -- in case the process is no longer running
                            catch (ArgumentException) { }
                        }
                    }
                    else throw new Exception("Could not list processes locking resource.");
                }
                else if (res != 0) throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            finally
            {
                RmEndSession(handle);
            }

            return processes;
        }
    }

}