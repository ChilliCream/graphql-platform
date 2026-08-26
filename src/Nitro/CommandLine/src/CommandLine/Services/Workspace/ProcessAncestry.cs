using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads a process's parent and name on Linux, macOS, and Windows, so the
/// ancestor walk that identifies a harness session works on all three.
/// Linux reads procfs directly; the others take one process-table snapshot
/// and answer every lookup from it, since a single CLI invocation walks only
/// a handful of pids.
/// </summary>
internal static class ProcessAncestry
{
    private static IReadOnlyDictionary<int, ProcessEntry>? s_snapshot;

    /// <summary>
    /// The parent pid of <paramref name="pid"/>, or null when it cannot be
    /// determined: the process is gone, access was denied, or the platform
    /// reports nothing for it.
    /// </summary>
    public static int? GetParentPid(int pid)
    {
        if (OperatingSystem.IsLinux())
        {
            return ReadLinuxParentPid(pid);
        }

        return Snapshot().TryGetValue(pid, out var entry) ? entry.ParentPid : null;
    }

    /// <summary>
    /// The executable name of <paramref name="pid"/> without a path or
    /// extension, or null when it cannot be determined.
    /// </summary>
    public static string? GetProcessName(int pid)
    {
        if (OperatingSystem.IsLinux())
        {
            return ReadLinuxProcessName(pid);
        }

        return Snapshot().TryGetValue(pid, out var entry) ? entry.Name : null;
    }

    private static int? ReadLinuxParentPid(int pid)
    {
        try
        {
            foreach (var line in File.ReadLines($"/proc/{pid}/status"))
            {
                if (line.StartsWith("PPid:", StringComparison.Ordinal))
                {
                    return int.TryParse(line["PPid:".Length..].Trim(), out var parsed) ? parsed : null;
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }

    private static string? ReadLinuxProcessName(int pid)
    {
        try
        {
            var path = $"/proc/{pid}/comm";

            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<int, ProcessEntry> Snapshot()
        => s_snapshot ??= OperatingSystem.IsWindows() ? SnapshotWindows() : SnapshotUnix();

    /// <summary>
    /// One <c>ps</c> listing of every process's pid, parent pid, and command
    /// name. Used on macOS and any other Unix without procfs.
    /// </summary>
    private static IReadOnlyDictionary<int, ProcessEntry> SnapshotUnix()
    {
        var entries = new Dictionary<int, ProcessEntry>();

        try
        {
            using var process = Process.Start(
                new ProcessStartInfo("ps")
                {
                    ArgumentList = { "-Ao", "pid=,ppid=,comm=" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                });

            if (process is null)
            {
                return entries;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts.Length < 2
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parentPid))
                {
                    continue;
                }

                var name = parts.Length > 2 ? Path.GetFileName(parts[2].Trim()) : null;
                entries[pid] = new ProcessEntry(parentPid, name);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // No `ps`, or it failed: the walk simply finds no ancestor.
        }

        return entries;
    }

    /// <summary>
    /// One Toolhelp32 process snapshot, the parent pid source Windows offers
    /// without WMI.
    /// </summary>
    private static IReadOnlyDictionary<int, ProcessEntry> SnapshotWindows()
    {
        var entries = new Dictionary<int, ProcessEntry>();

        if (!OperatingSystem.IsWindows())
        {
            return entries;
        }

        var snapshot = CreateToolhelp32Snapshot(Th32CsSnapProcess, 0);

        if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
        {
            return entries;
        }

        try
        {
            var entry = new ProcessEntry32 { Size = (uint)Marshal.SizeOf<ProcessEntry32>() };

            if (!Process32FirstW(snapshot, ref entry))
            {
                return entries;
            }

            do
            {
                entries[(int)entry.ProcessId] =
                    new ProcessEntry((int)entry.ParentProcessId, Path.GetFileNameWithoutExtension(entry.ExeFile));
            }
            while (Process32NextW(snapshot, ref entry));
        }
        finally
        {
            CloseHandle(snapshot);
        }

        return entries;
    }

    private const uint Th32CsSnapProcess = 0x00000002;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32FirstW(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32NextW(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    private sealed record ProcessEntry(int ParentPid, string? Name);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public IntPtr DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExeFile;
    }
}
