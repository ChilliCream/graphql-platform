using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.Aspire;

internal static class FusionReleaseStore
{
    internal static JsonSerializerOptions SerializerOptions { get; } =
        CreateSerializerOptions();

    public static async Task<FusionReleaseManifest> ReadFinalAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        if (!Path.IsPathFullyQualified(manifestPath))
        {
            throw new InvalidOperationException(
                "The Fusion release manifest parameter must resolve to an absolute path.");
        }

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                "The promoted Fusion release manifest was not found.",
                manifestPath);
        }

        try
        {
            await using var stream = File.OpenRead(manifestPath);
            var manifest =
                await JsonSerializer.DeserializeAsync<FusionReleaseManifest>(
                    stream,
                    SerializerOptions,
                    cancellationToken)
                ?? throw new InvalidDataException(
                    "The Fusion release manifest is empty.");

            Validate(manifest, requireTargets: true);
            return manifest;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The Fusion release manifest contains invalid JSON.",
                exception);
        }
    }

    public static async Task<FusionReleaseManifest> ReadDraftAsync(
        string releaseDirectory,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            releaseDirectory,
            "fusion-release.draft.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "The Fusion release draft manifest was not found.",
                path);
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var manifest =
                await JsonSerializer.DeserializeAsync<FusionReleaseManifest>(
                    stream,
                    SerializerOptions,
                    cancellationToken)
                ?? throw new InvalidDataException(
                    "The Fusion release draft manifest is empty.");
            Validate(manifest, requireTargets: false);
            return manifest;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The Fusion release draft manifest contains invalid JSON.",
                exception);
        }
    }

    public static Task WriteDraftAsync(
        string releaseDirectory,
        FusionReleaseManifest manifest,
        CancellationToken cancellationToken)
    {
        Validate(manifest, requireTargets: false);
        return WriteAtomicallyAsync(
            Path.Combine(releaseDirectory, "fusion-release.draft.json"),
            manifest,
            overwrite: true,
            cancellationToken);
    }

    public static Task WriteFinalAsync(
        string releaseDirectory,
        FusionReleaseManifest manifest,
        CancellationToken cancellationToken)
    {
        Validate(manifest, requireTargets: true);
        return WriteAtomicallyAsync(
            Path.Combine(releaseDirectory, "fusion-release.json"),
            manifest,
            overwrite: false,
            cancellationToken);
    }

    public static string ResolveArchivePath(
        string manifestPath,
        FusionReleaseSource source)
    {
        if (Path.IsPathFullyQualified(source.ArchivePath))
        {
            throw new InvalidDataException(
                $"Fusion source '{source.Name}' archive path must be relative.");
        }

        var manifestDirectory = Path.GetDirectoryName(
            Path.GetFullPath(manifestPath))!;
        var archivePath = Path.GetFullPath(
            source.ArchivePath,
            manifestDirectory);
        var expectedPrefix = manifestDirectory.EndsWith(
            Path.DirectorySeparatorChar)
            ? manifestDirectory
            : manifestDirectory + Path.DirectorySeparatorChar;

        if (!archivePath.StartsWith(
                expectedPrefix,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Fusion source '{source.Name}' archive path escapes the release directory.");
        }

        return archivePath;
    }

    public static async Task VerifyArchiveAsync(
        string archivePath,
        FusionReleaseSource source,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException(
                $"Fusion source '{source.Name}' archive was not found.",
                archivePath);
        }

        await using var stream = File.OpenRead(archivePath);
        var digest = Convert.ToHexStringLower(
            await SHA256.HashDataAsync(stream, cancellationToken));

        if (!string.Equals(
                digest,
                source.ArchiveSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Fusion source '{source.Name}' archive SHA-256 does not match the release manifest.");
        }
    }

    public static void Validate(
        FusionReleaseManifest manifest,
        bool requireTargets)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (manifest.FormatVersion is not FusionReleaseManifest.CurrentFormatVersion)
        {
            throw new InvalidDataException(
                $"Unsupported Fusion release manifest format version '{manifest.FormatVersion}'.");
        }

        if (manifest.Composition is null
            || manifest.Composition.Settings is null
            || manifest.Composition.Settings.ExcludeByTag is null
            || manifest.Sources is null
            || manifest.Targets is null)
        {
            throw new InvalidDataException(
                "The Fusion release manifest is missing required content.");
        }

        ValidatePathSegment(manifest.ReleaseId, "release ID");
        if (string.IsNullOrWhiteSpace(manifest.CompositionToolVersion))
        {
            throw new InvalidDataException(
                "The Fusion release manifest is missing the composition tool version.");
        }

        ValidateSha256(manifest.SourceSetSha256, "source set");
        ValidateSha256(
            manifest.Composition.SettingsSha256,
            "composition settings");

        var expectedCompositionDigest =
            FusionReleaseDigests.ComputeCompositionSha256(
                manifest.Composition.Settings);
        if (!string.Equals(
                expectedCompositionDigest,
                manifest.Composition.SettingsSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "The Fusion release composition settings digest is invalid.");
        }

        if (manifest.Sources.Count == 0)
        {
            throw new InvalidDataException(
                "The Fusion release manifest must contain at least one source.");
        }

        if (manifest.Sources.Any(source =>
                source is null
                || string.IsNullOrWhiteSpace(source.Name)
                || string.IsNullOrWhiteSpace(source.Version)
                || string.IsNullOrWhiteSpace(source.ArchivePath)
                || string.IsNullOrWhiteSpace(source.ArchiveSha256)
                || string.IsNullOrWhiteSpace(source.ContentSha256)))
        {
            throw new InvalidDataException(
                "The Fusion release manifest contains an invalid source.");
        }

        var duplicateSource = manifest.Sources
            .GroupBy(source => source.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSource is not null)
        {
            throw new InvalidDataException(
                $"Fusion release source '{duplicateSource.Key}' is duplicated.");
        }

        foreach (var source in manifest.Sources)
        {
            ValidatePathSegment(source.Name, "source name");
            ValidatePathSegment(source.Version, "source version");
            ValidateSha256(
                source.ArchiveSha256,
                $"source '{source.Name}' archive");
            ValidateSha256(
                source.ContentSha256,
                $"source '{source.Name}' content");

            if (source.ArchivePath.Contains('\\')
                || Path.IsPathFullyQualified(source.ArchivePath)
                || source.ArchivePath.Split(
                        [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                        StringSplitOptions.RemoveEmptyEntries)
                    .Any(segment => segment is "." or ".."))
            {
                throw new InvalidDataException(
                    $"Fusion source '{source.Name}' archive path must remain inside the release.");
            }
        }

        var expectedSourceSetDigest =
            FusionReleaseDigests.ComputeSourceSetSha256(manifest.Sources);
        if (!string.Equals(
                expectedSourceSetDigest,
                manifest.SourceSetSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "The Fusion release source set digest is invalid.");
        }

        if (requireTargets && manifest.Targets.Count == 0)
        {
            throw new InvalidDataException(
                "The final Fusion release manifest must contain an uploaded target.");
        }

        if (manifest.Targets.Any(target =>
                target is null
                || string.IsNullOrWhiteSpace(target.CloudUrl)
                || string.IsNullOrWhiteSpace(target.ApiId)
                || string.IsNullOrWhiteSpace(target.SourceSetSha256)
                || target.Sources is null
                || target.Sources.Any(source =>
                    source is null
                    || string.IsNullOrWhiteSpace(source.Name)
                    || string.IsNullOrWhiteSpace(source.Version)
                    || string.IsNullOrWhiteSpace(source.ContentSha256))))
        {
            throw new InvalidDataException(
                "The Fusion release manifest contains an invalid target.");
        }

        var duplicateTarget = manifest.Targets
            .GroupBy(
                target => (target.CloudUrl, target.ApiId),
                FusionReleaseTargetKeyComparer.Instance)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTarget is not null)
        {
            throw new InvalidDataException(
                $"Fusion release target '{duplicateTarget.Key.ApiId}' is duplicated.");
        }

        foreach (var target in manifest.Targets)
        {
            if (target is null
                || target.Sources is null
                || target.Sources.Any(source => source is null))
            {
                throw new InvalidDataException(
                    "The Fusion release manifest contains an invalid target.");
            }

            if (!Uri.TryCreate(
                    target.CloudUrl,
                    UriKind.Absolute,
                    out var cloudUri)
                || cloudUri.Scheme is not "https"
                || !string.IsNullOrEmpty(cloudUri.UserInfo)
                || cloudUri.AbsolutePath is not "/"
                || !string.IsNullOrEmpty(cloudUri.Query)
                || !string.IsNullOrEmpty(cloudUri.Fragment))
            {
                throw new InvalidDataException(
                    $"Fusion release target '{target.ApiId}' cloud URL must be "
                    + "an absolute HTTPS origin.");
            }

            if (string.IsNullOrWhiteSpace(target.ApiId))
            {
                throw new InvalidDataException(
                    "A Fusion release target must specify an API ID.");
            }

            if (!string.Equals(
                    target.SourceSetSha256,
                    manifest.SourceSetSha256,
                    StringComparison.OrdinalIgnoreCase)
                || target.Sources.Count != manifest.Sources.Count)
            {
                throw new InvalidDataException(
                    $"Fusion release target '{target.ApiId}' does not contain the release source set.");
            }

            foreach (var source in manifest.Sources)
            {
                var reference = target.Sources.SingleOrDefault(candidate =>
                    string.Equals(candidate.Name, source.Name, StringComparison.Ordinal));
                if (reference is null
                    || !string.Equals(
                        reference.Version,
                        source.Version,
                        StringComparison.Ordinal)
                    || !string.Equals(
                        reference.ContentSha256,
                        source.ContentSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Fusion release target '{target.ApiId}' source set does not match the release.");
                }
            }
        }
    }

    private static async Task WriteAtomicallyAsync(
        string path,
        FusionReleaseManifest manifest,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            manifest,
            SerializerOptions);

        if (!overwrite && File.Exists(path))
        {
            var current = await File.ReadAllBytesAsync(
                path,
                cancellationToken);
            if (current.AsSpan().SequenceEqual(bytes))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Fusion release manifest '{path}' already exists with different content.");
        }

        var temporaryPath =
            path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(
                temporaryPath,
                bytes,
                cancellationToken);
            File.Move(temporaryPath, path, overwrite);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void ValidatePathSegment(
        string value,
        string description)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value is "." or ".."
            || value.Any(character =>
                !char.IsAsciiLetterOrDigit(character)
                && character is not '.' and not '_' and not '-'))
        {
            throw new InvalidDataException(
                $"Fusion {description} '{value}' is not a portable path segment.");
        }
    }

    private static void ValidateSha256(
        string value,
        string description)
    {
        if (string.IsNullOrEmpty(value)
            || value.Length is not 64
            || !value.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException(
                $"Fusion {description} SHA-256 must contain 64 hexadecimal characters.");
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class FusionReleaseTargetKeyComparer
        : IEqualityComparer<(string CloudUrl, string ApiId)>
    {
        public static FusionReleaseTargetKeyComparer Instance { get; } = new();

        public bool Equals(
            (string CloudUrl, string ApiId) x,
            (string CloudUrl, string ApiId) y)
            => string.Equals(
                    x.CloudUrl.TrimEnd('/'),
                    y.CloudUrl.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ApiId, y.ApiId, StringComparison.Ordinal);

        public int GetHashCode((string CloudUrl, string ApiId) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(
                    obj.CloudUrl.TrimEnd('/')),
                StringComparer.Ordinal.GetHashCode(obj.ApiId));
    }
}
