using System.Diagnostics;
using System.Runtime.InteropServices;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class PingWorkerLauncher : IPingWorkerLauncher
{
    public bool TryLaunch(LaunchDescriptor descriptor, IReadOnlyList<string> workerArgs)
    {
        try
        {
            var startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? BuildLinuxDetachedStartInfo(descriptor, workerArgs)
                : BuildFallbackStartInfo(descriptor, workerArgs);

            using var process = Process.Start(startInfo);

            // Fully redirected (never inherits this process's stdio), and
            // the worker itself never writes to its own stdout/stderr - so
            // leaving the pipes unread here is safe: nothing ever fills
            // them, and this process exits (closing its ends) long before
            // the detached child would have anything to say.
            return process is not null;
        }
        catch
        {
            // Fail-open: no "setsid" on PATH, permission denied, or any
            // other spawn-time failure. The caller records this as
            // spawn-failed and releases the lease it already holds.
            return false;
        }
    }

    /// <summary>
    /// <c>setsid nohup &lt;nitro&gt; ...</c>: the exact idiom
    /// perles-net-k3j.3's spike verified survives across Bash-tool-call
    /// boundaries. <c>setsid</c> starts the child in a new session with no
    /// controlling terminal; <c>nohup</c> additionally ignores SIGHUP, in
    /// case something downstream still tries to signal the session leader.
    /// </summary>
    // Internal, not private: PingWorkerLauncherTests asserts the built
    // ProcessStartInfo's environment countermeasures directly.
    internal static ProcessStartInfo BuildLinuxDetachedStartInfo(
        LaunchDescriptor descriptor, IReadOnlyList<string> workerArgs)
    {
        var startInfo = new ProcessStartInfo("setsid")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("nohup");
        startInfo.ArgumentList.Add(descriptor.Executable);

        foreach (var arg in descriptor.ArgumentPrefix)
        {
            startInfo.ArgumentList.Add(arg);
        }

        foreach (var arg in workerArgs)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.Environment.Remove("NITRO_MAIL_ACTOR");
        startInfo.Environment.Remove("NITRO_TASK_ACTOR");
        startInfo.Environment["NITRO_HOOK_SUPPRESS"] = "1";

        return startInfo;
    }

    /// <summary>
    /// Non-Linux best effort: no session-detachment primitive is used here,
    /// so survival across a terminating parent session is not guaranteed
    /// (a documented, accepted limitation, matching the ancestor-walk's
    /// Linux-first stance elsewhere in this codebase).
    /// </summary>
    internal static ProcessStartInfo BuildFallbackStartInfo(
        LaunchDescriptor descriptor, IReadOnlyList<string> workerArgs)
    {
        var startInfo = new ProcessStartInfo(descriptor.Executable)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in descriptor.ArgumentPrefix)
        {
            startInfo.ArgumentList.Add(arg);
        }

        foreach (var arg in workerArgs)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.Environment.Remove("NITRO_MAIL_ACTOR");
        startInfo.Environment.Remove("NITRO_TASK_ACTOR");
        startInfo.Environment["NITRO_HOOK_SUPPRESS"] = "1";

        return startInfo;
    }
}
