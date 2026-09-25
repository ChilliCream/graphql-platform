using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed partial class NitroInstanceIdProvider(
    IFileSystem fileSystem,
    Func<string?>? machineIdReader = null) : INitroInstanceIdProvider
{
    private const string FallbackFileName = "instance-id";

    private static readonly string[] s_linuxMachineIdPaths = ["/etc/machine-id", "/var/lib/dbus/machine-id"];

    private readonly Func<string?> _machineIdReader = machineIdReader ?? ReadPlatformMachineId;

    public async Task<string> GetIdAsync(string globalConfigDirectory, CancellationToken cancellationToken)
    {
        var machineId = _machineIdReader();

        return machineId is not null
            ? HashMachineId(machineId)
            : await GetOrCreateFallbackIdAsync(globalConfigDirectory, cancellationToken);
    }

    /// <summary>
    /// Returns the platform machine identifier, or null when it is unavailable or reading it fails.
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
        foreach (var path in s_linuxMachineIdPaths)
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
    /// Returns standard output when reading and process exit complete within two seconds.
    /// On timeout, attempts to terminate the process and returns null.
    /// </summary>
    private static string? ReadWithTimeout(Process process)
    {
        var stopwatch = Stopwatch.StartNew();
        var readTask = process.StandardOutput.ReadToEndAsync();

        if (!readTask.Wait(2000))
        {
            TryKill(process);
            return null;
        }

        var remaining = Math.Max(0, 2000 - (int)stopwatch.ElapsedMilliseconds);

        if (!process.WaitForExit(remaining))
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
            // Process termination is best effort.
        }
    }

    private static string HashMachineId(string machineId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(machineId));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Returns the persisted fallback id or atomically creates one when absent.
    /// If another process creates it first, returns that id.
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
