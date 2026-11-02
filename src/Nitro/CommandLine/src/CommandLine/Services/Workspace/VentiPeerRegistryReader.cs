using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Finds the peer endpoint a Venti session advertises in Venti's peer registry
/// (<c>~/.venti/peers/&lt;session id&gt;.json</c>). Only an interactive root session speaking
/// peer protocol 1 advertises one; its session id is the endpoint name, and the version is empty.
/// </summary>
internal sealed class VentiPeerRegistryReader : IClaudeSessionFileReader
{
    /// <summary>
    /// Venti's peer registry directory for the current user.
    /// </summary>
    public static string DefaultDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".venti", "peers");

    private readonly string _directory;

    public VentiPeerRegistryReader() : this(DefaultDirectory)
    {
    }

    internal VentiPeerRegistryReader(string directory)
    {
        _directory = directory;
    }

    public ClaudeSessionFile? Find(string sessionId)
    {
        if (!EndpointAddress.IsValid(sessionId))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, sessionId + ".json")));
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("sessionId", out var id)
                || id.ValueKind != JsonValueKind.String
                || id.GetString() != sessionId
                || !root.TryGetProperty("kind", out var kind)
                || kind.ValueKind != JsonValueKind.String
                || kind.GetString() != "interactive"
                || !root.TryGetProperty("messagingSocketPath", out var endpoint)
                || endpoint.ValueKind != JsonValueKind.String
                || string.IsNullOrEmpty(endpoint.GetString())
                || !root.TryGetProperty("peerProtocol", out var protocol)
                || protocol.ValueKind != JsonValueKind.Number
                || !protocol.TryGetInt32(out var protocolVersion)
                || protocolVersion != ClaudePeerClient.SupportedProtocol)
            {
                return null;
            }

            return new ClaudeSessionFile(sessionId, Cwd: string.Empty, Name: sessionId, Version: string.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A missing, unreadable, or half-written registry file advertises no endpoint.
            return null;
        }
    }
}
