using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves this machine's Nitro instance id: the value stored in
/// <c>agent_sessions.host</c> so pid liveness checks and reaping can tell a
/// session spawned on this host apart from a row left behind by another
/// machine sharing the same workspace over a network drive or a synced home
/// directory. A naive `~/.nitro/instance-id` would roam with a cloned or
/// shared home and recreate the exact cross-host hazard this exists to
/// prevent, so the id is anchored to the machine, not the workspace or the
/// home directory.
/// </summary>
internal interface INitroInstanceIdProvider
{
    /// <summary>
    /// Returns the OS machine identifier, hashed, when one can be read.
    /// Otherwise falls back to a generated id persisted under
    /// <paramref name="globalConfigDirectory"/> (the platform application
    /// data directory Nitro already uses for global state), created with an
    /// atomic create-or-read-winner so concurrent first use across processes
    /// yields one id. Regenerating the fallback (a fresh machine image, a
    /// deleted app-data directory) strands old rows as remote; that is
    /// accepted, documented behavior, not a bug this method needs to guard
    /// against.
    /// </summary>
    Task<string> GetIdAsync(string globalConfigDirectory, CancellationToken cancellationToken);
}

internal sealed partial class NitroInstanceIdProvider(
    IFileSystem fileSystem,
    Func<string?>? machineIdReader = null) : INitroInstanceIdProvider
{
    private const string FallbackFileName = "instance-id";

    private static readonly string[] LinuxMachineIdPaths = ["/etc/machine-id", "/var/lib/dbus/machine-id"];

    private readonly Func<string?> _machineIdReader = machineIdReader ?? ReadPlatformMachineId;

    public async Task<string> GetIdAsync(string globalConfigDirectory, CancellationToken cancellationToken)
    {
        var machineId = _machineIdReader();

        return machineId is not null
            ? HashMachineId(machineId)
            : await GetOrCreateFallbackIdAsync(globalConfigDirectory, cancellationToken);
    }

    /// <summary>
    /// Dispatches to the platform-specific machine identifier reader. Any
    /// failure (missing file, denied registry access, no <c>ioreg</c> on a
    /// stripped-down macOS build) is swallowed here: it just means this
    /// method falls through to the generated-id fallback, which is the
    /// documented, always-available path.
    /// </summary>
    private static string? ReadPlatformMachineId()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return ReadLinuxMachineId();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ReadWindowsMachineGuid();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return ReadMacPlatformUuid();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? ReadLinuxMachineId()
    {
        foreach (var path in LinuxMachineIdPaths)
        {
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path).Trim();

                if (content.Length > 0)
                {
                    return content;
                }
            }
        }

        return null;
    }

    private static string? ReadWindowsMachineGuid()
    {
        using var process = Process.Start(new ProcessStartInfo("reg")
        {
            ArgumentList = { "query", """HKLM\SOFTWARE\Microsoft\Cryptography""", "/v", "MachineGuid" },
            RedirectStandardOutput = true,
            UseShellExecute = false
        });

        if (process is null)
        {
            return null;
        }

        var output = ReadWithTimeout(process);

        if (output is null)
        {
            return null;
        }

        var match = WindowsMachineGuidPattern().Match(output);

        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"MachineGuid\s+REG_SZ\s+(\S+)")]
    private static partial Regex WindowsMachineGuidPattern();

    private static string? ReadMacPlatformUuid()
    {
        using var process = Process.Start(new ProcessStartInfo("ioreg")
        {
            ArgumentList = { "-rd1", "-c", "IOPlatformExpertDevice" },
            RedirectStandardOutput = true,
            UseShellExecute = false
        });

        if (process is null)
        {
            return null;
        }

        var output = ReadWithTimeout(process);

        if (output is null)
        {
            return null;
        }

        var match = MacPlatformUuidPattern().Match(output);

        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(""""IOPlatformUUID"\s*=\s*"([^"]+)"""")]
    private static partial Regex MacPlatformUuidPattern();

    /// <summary>
    /// Reads a child process's standard output within a 2-second bound.
    /// Reading to end of stream BEFORE waiting for exit would make that
    /// bound unreachable if the child never closes its stdout: the read
    /// itself carries no timeout, so this drains stdout asynchronously
    /// (avoiding the classic redirect deadlock for a child that blocks
    /// writing to a full pipe) while enforcing the bound on the read, then
    /// confirms exit. A child still running past the bound is killed rather
    /// than left behind.
    /// </summary>
    private static string? ReadWithTimeout(Process process)
    {
        var readTask = process.StandardOutput.ReadToEndAsync();

        if (!readTask.Wait(2000) || !process.WaitForExit(2000))
        {
            TryKill(process);
            return null;
        }

        return readTask.GetAwaiter().GetResult();
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The process may have exited on its own between the timeout
            // check and this call; either way, nothing more to do here.
        }
    }

    private static string HashMachineId(string machineId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(machineId));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Atomic create-or-read-winner: try to atomically create the fallback
    /// id file with a freshly generated candidate; if another process (or an
    /// earlier run) already won that race, read back whatever it wrote
    /// instead of the candidate this call generated.
    /// </summary>
    private async Task<string> GetOrCreateFallbackIdAsync(
        string globalConfigDirectory,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(globalConfigDirectory, FallbackFileName);

        if (fileSystem.FileExists(path))
        {
            return (await fileSystem.ReadAllTextAsync(path, cancellationToken)).Trim();
        }

        if (!fileSystem.DirectoryExists(globalConfigDirectory))
        {
            fileSystem.CreateDirectory(globalConfigDirectory);
        }

        var candidate = Guid.NewGuid().ToString("N");

        try
        {
            await fileSystem.CreateFileAtomicAsync(path, candidate, cancellationToken);

            return candidate;
        }
        catch (IOException)
        {
            return (await fileSystem.ReadAllTextAsync(path, cancellationToken)).Trim();
        }
    }
}
