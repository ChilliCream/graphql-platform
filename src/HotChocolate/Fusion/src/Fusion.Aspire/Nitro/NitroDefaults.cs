using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The production values for the ambient dependencies of the Nitro access layer. Every component
/// takes its dependencies as required constructor parameters, and this is the single place the
/// composition root reads the production values from.
/// </summary>
internal static class NitroDefaults
{
    private const string ConfigDirectoryName = "nitro";
    private const string SessionFileName = "session.json";
    private const string CacheDirectoryName = "cache";
    private const string FusionCacheDirectoryName = "fusion";
    private const string RunSeedDirectoryName = "hc-fusion-aspire-nitro";

    /// <summary>
    /// The Nitro API URL that is used when neither the environment nor the selected access
    /// token configures one.
    /// </summary>
    public static readonly Uri ApiUrl = new("https://api.chillicream.com");

    /// <summary>
    /// The delay before the single re-read of the session file that resolves a read which raced
    /// with <c>nitro login</c>.
    /// </summary>
    public static readonly TimeSpan SessionRereadDelay = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// The window before the access token expiry in which the token is already treated as
    /// expired.
    /// </summary>
    public static readonly TimeSpan AccessTokenExpiryGrace = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The retry budget for fusion configuration downloads.
    /// </summary>
    public static readonly NitroDownloadRetryPolicy DownloadRetryPolicy =
        new(attemptsWithCachedSeed: 2, attemptsWithoutCachedSeed: 15, TimeSpan.FromSeconds(2));

    /// <summary>
    /// Gets the directory that the Nitro CLI keeps its configuration in.
    /// </summary>
    public static string GetConfigDirectory()
        => IOPath.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ConfigDirectoryName);

    /// <summary>
    /// Gets the full path of the session file that the Nitro CLI writes on <c>nitro login</c>.
    /// </summary>
    public static string GetSessionFilePath()
        => IOPath.Combine(GetConfigDirectory(), SessionFileName);

    /// <summary>
    /// Gets the directory that holds the cached fusion configurations. It sits next to the Nitro
    /// CLI configuration rather than in the AppHost output, so it survives a clean and is shared
    /// across worktrees.
    /// </summary>
    public static string GetSeedCacheDirectory()
        => IOPath.Combine(GetConfigDirectory(), CacheDirectoryName, FusionCacheDirectoryName);

    /// <summary>
    /// Creates the path of the directory that holds the fusion configurations the gateways of a
    /// run compose against. Every run gets its own directory, so two runs on the same machine
    /// never share a fusion configuration.
    /// </summary>
    public static string CreateRunSeedDirectoryPath()
        => IOPath.Combine(
            IOPath.GetTempPath(),
            RunSeedDirectoryName,
            Guid.NewGuid().ToString("N"));
}
