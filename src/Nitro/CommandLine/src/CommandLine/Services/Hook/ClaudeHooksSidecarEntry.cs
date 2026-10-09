using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The command, timeout, hash, and installation time recorded for one Claude hook event.
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
