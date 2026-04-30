namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Sandbox permissions requested by the MCP App View resource. Hosts MAY honor these by setting
/// appropriate iframe <c>allow</c> attributes. Apps SHOULD NOT assume permissions are granted; use
/// JS feature detection as fallback.
/// </summary>
public sealed class McpAppViewPermissions
{
    /// <summary>
    /// Request camera access (Permission Policy <c>camera</c> feature).
    /// </summary>
    public bool? Camera { get; init; }

    /// <summary>
    /// Request clipboard write access (Permission Policy <c>clipboard-write</c> feature).
    /// </summary>
    public bool? ClipboardWrite { get; init; }

    /// <summary>
    /// Request geolocation access (Permission Policy <c>geolocation</c> feature).
    /// </summary>
    public bool? Geolocation { get; init; }

    /// <summary>
    /// Request microphone access (Permission Policy <c>microphone</c> feature).
    /// </summary>
    public bool? Microphone { get; init; }
}
