namespace ChilliCream.Nitro.Fusion;

/// <summary>
/// An exact immutable source schema archive downloaded from Nitro.
/// </summary>
public sealed record FusionSourceSchemaDownload(
    string Name,
    string Version,
    ReadOnlyMemory<byte> Archive,
    string ContentSha256);
