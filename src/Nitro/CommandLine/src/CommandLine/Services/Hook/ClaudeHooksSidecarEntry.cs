using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// One event's recorded installation: the exact command and timeout this
/// CLI wrote, and their hash. Provenance, not just detection - unlike
/// <see cref="ClaudeHooksTemplate.CommandMarker"/> matching (which any
/// Nitro-owned entry satisfies, on any machine, from any version), a
/// sidecar record is proof THIS install wrote THIS exact entry, which is
/// what makes uninstall able to remove precisely what it installed instead
/// of falling back to marker matching.
/// </summary>
internal sealed record ClaudeHooksSidecarEntry(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("timeoutSeconds")] int TimeoutSeconds,
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("installedAt")] DateTimeOffset InstalledAt)
{
    public static string ComputeHash(string command, int timeoutSeconds)
    {
        var canonical = $"{command}\n{timeoutSeconds}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
