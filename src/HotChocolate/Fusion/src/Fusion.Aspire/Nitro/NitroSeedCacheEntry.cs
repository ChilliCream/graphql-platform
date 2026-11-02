namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// A cached fusion configuration that was validated as a fusion archive.
/// </summary>
/// <param name="FilePath">
/// The full path of the cached fusion archive.
/// </param>
/// <param name="DownloadedAt">
/// The point in time at which the fusion configuration was downloaded, which tells the developer
/// how old a fallback is.
/// </param>
internal sealed record NitroSeedCacheEntry(string FilePath, DateTimeOffset DownloadedAt);
