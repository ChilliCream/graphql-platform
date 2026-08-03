using System.Security.Cryptography;
using System.Text.Json;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The last known good fusion configuration per Nitro API, api and stage. The cache lets a
/// gateway start while Nitro cannot be reached.
/// </summary>
/// <remarks>
/// A download is promoted into the cache only after it validated as a fusion archive, and the
/// promotion replaces the entry in a single move, so a failed or invalid download never destroys
/// the previous entry. Entries are validated again when they are read, and an entry that no
/// longer validates is discarded and reported as absent. The metadata carries a marker of the
/// file it describes, so an entry whose archive and metadata do not belong together, which is
/// what an interrupted promotion leaves behind, is discarded as well.
/// </remarks>
internal sealed class NitroSeedCache
{
    private const string ArchiveExtension = ".far";
    private const string MetadataExtension = ".json";
    private const string TempExtension = ".tmp";

    private readonly string _cacheDirectory;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroSeedCache"/>.
    /// </summary>
    /// <param name="cacheDirectory">
    /// The directory that holds the cached fusion configurations.
    /// </param>
    /// <param name="timeProvider">
    /// The time source that stamps a promoted entry.
    /// </param>
    public NitroSeedCache(string cacheDirectory, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheDirectory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _cacheDirectory = cacheDirectory;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the directory that holds the cached fusion configurations.
    /// </summary>
    public string CacheDirectory => _cacheDirectory;

    /// <summary>
    /// Gets the cached fusion configuration for a key.
    /// </summary>
    /// <param name="key">
    /// The key of the entry.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a warning when an entry is discarded.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The entry, or <c>null</c> when no entry exists or the entry was discarded.
    /// </returns>
    public async Task<NitroSeedCacheEntry?> TryGetAsync(
        NitroSeedKey key,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(logger);

        var archivePath = GetArchivePath(key);

        if (!File.Exists(archivePath))
        {
            return null;
        }

        var metadata = await TryReadMetadataAsync(GetMetadataPath(key), cancellationToken);

        if (metadata is null)
        {
            Discard(key, logger, "its metadata is missing or could not be read");
            return null;
        }

        if (!string.Equals(metadata.ApiUrl, key.ApiUrl.AbsoluteUri, StringComparison.Ordinal)
            || !string.Equals(metadata.ApiId, key.ApiId, StringComparison.Ordinal)
            || !string.Equals(metadata.Stage, key.Stage, StringComparison.Ordinal))
        {
            Discard(key, logger, "its metadata describes another api url, api or stage");
            return null;
        }

        if (!string.Equals(metadata.FusionVersion, CurrentFusionVersion, StringComparison.Ordinal))
        {
            Discard(
                key,
                logger,
                $"it was downloaded for the gateway format version {metadata.FusionVersion} "
                + $"instead of {CurrentFusionVersion}");
            return null;
        }

        if (!await IsFusionArchiveAsync(archivePath, cancellationToken))
        {
            Discard(key, logger, "it is not a valid fusion archive");
            return null;
        }

        var contentMarker = await TryCreateContentMarkerAsync(archivePath, cancellationToken);

        if (contentMarker is null
            || !string.Equals(metadata.ContentMarker, contentMarker, StringComparison.Ordinal))
        {
            Discard(
                key,
                logger,
                "its metadata does not describe the cached file, which means a previous download "
                + "was interrupted while it was promoted into the cache");
            return null;
        }

        return new NitroSeedCacheEntry(archivePath, metadata.DownloadedAt);
    }

    /// <summary>
    /// Creates the path of a temporary file in the cache directory that a download is streamed
    /// into. The file is on the same volume as the cache entry, so promoting it is a move.
    /// </summary>
    /// <param name="key">
    /// The key of the entry the download belongs to.
    /// </param>
    public string CreateTempFilePath(NitroSeedKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        Directory.CreateDirectory(_cacheDirectory);

        return IOPath.Combine(
            _cacheDirectory,
            $"{key.Hash}.{Guid.NewGuid():N}{TempExtension}");
    }

    /// <summary>
    /// Promotes a downloaded file into the cache after validating it as a fusion archive.
    /// </summary>
    /// <param name="key">
    /// The key of the entry.
    /// </param>
    /// <param name="tempFilePath">
    /// The path of the downloaded file, created with <see cref="CreateTempFilePath"/>.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a warning when the download does not validate.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The promoted entry, or <c>null</c> when the download did not validate as a fusion archive.
    /// In that case a previously cached entry is kept.
    /// </returns>
    public async Task<NitroSeedCacheEntry?> TryPromoteAsync(
        NitroSeedKey key,
        string tempFilePath,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempFilePath);
        ArgumentNullException.ThrowIfNull(logger);

        if (!await IsFusionArchiveAsync(tempFilePath, cancellationToken))
        {
            logger.LogWarning(
                "The fusion configuration that was downloaded for the api {ApiId} and the stage "
                + "{Stage} from {ApiUrl} is not a valid fusion archive and was discarded.",
                key.ApiId,
                key.Stage,
                key.ApiUrl);

            TryDelete(tempFilePath);

            return null;
        }

        var downloadedAt = _timeProvider.GetUtcNow();
        var contentMarker = await TryCreateContentMarkerAsync(tempFilePath, cancellationToken);

        if (contentMarker is null)
        {
            logger.LogWarning(
                "The fusion configuration that was downloaded for the api {ApiId} and the stage "
                + "{Stage} from {ApiUrl} could not be read and was discarded.",
                key.ApiId,
                key.Stage,
                key.ApiUrl);

            TryDelete(tempFilePath);

            return null;
        }

        Directory.CreateDirectory(_cacheDirectory);
        File.Move(tempFilePath, GetArchivePath(key), overwrite: true);

        var metadata = new NitroSeedMetadata
        {
            ApiUrl = key.ApiUrl.AbsoluteUri,
            ApiId = key.ApiId,
            Stage = key.Stage,
            DownloadedAt = downloadedAt,
            FusionVersion = CurrentFusionVersion,
            ContentMarker = contentMarker
        };

        await using var stream = File.Create(GetMetadataPath(key));
        await JsonSerializer.SerializeAsync(
            stream,
            metadata,
            NitroJsonContext.Default.NitroSeedMetadata,
            cancellationToken);

        return new NitroSeedCacheEntry(GetArchivePath(key), downloadedAt);
    }

    /// <summary>
    /// Deletes a temporary file that was not promoted.
    /// </summary>
    /// <param name="tempFilePath">
    /// The path of the temporary file.
    /// </param>
    public void DeleteTempFile(string tempFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tempFilePath);

        TryDelete(tempFilePath);
    }

    internal string GetArchivePath(NitroSeedKey key)
        => IOPath.Combine(_cacheDirectory, key.Hash + ArchiveExtension);

    internal string GetMetadataPath(NitroSeedKey key)
        => IOPath.Combine(_cacheDirectory, key.Hash + MetadataExtension);

    private static string CurrentFusionVersion
        => WellKnownVersions.LatestGatewayFormatVersion.ToString();

    private void Discard(NitroSeedKey key, ILogger logger, string reason)
    {
        logger.LogWarning(
            "The cached fusion configuration for the api {ApiId} and the stage {Stage} at "
            + "{FilePath} was discarded because {Reason}.",
            key.ApiId,
            key.Stage,
            GetArchivePath(key),
            reason);

        TryDelete(GetArchivePath(key));
        TryDelete(GetMetadataPath(key));
    }

    private static async Task<NitroSeedMetadata?> TryReadMetadataAsync(
        string metadataPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(metadataPath);

            return await JsonSerializer.DeserializeAsync(
                stream,
                NitroJsonContext.Default.NitroSeedMetadata,
                cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates the marker that ties the metadata of an entry to the file it describes. The archive
    /// and its metadata are written one after the other, so an interrupted promotion can leave a
    /// new archive next to the metadata of the previous one.
    /// </summary>
    private static async Task<string?> TryCreateContentMarkerAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);

            return Convert.ToHexStringLower(hash);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static async Task<bool> IsFusionArchiveAsync(
        string archivePath,
        CancellationToken cancellationToken)
    {
        try
        {
            using var archive = FusionArchive.Open(archivePath);
            var sourceSchemaNames = await archive.GetSourceSchemaNamesAsync(cancellationToken);

            return sourceSchemaNames.Any();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A file that cannot be deleted is left behind. The next promotion overwrites it.
        }
    }
}
