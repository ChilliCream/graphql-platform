namespace ChilliCream.Nitro.Client;

/// <summary>
/// Optional source metadata for publish/upload operations.
/// </summary>
public sealed record SourceMetadata(SourceGitHubMetadata? GitHub = null);
