using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Aspire;
using HotChocolate.Fusion.Options;

namespace ChilliCream.Nitro.Aspire;

internal sealed record FusionReleaseManifest(
    int FormatVersion,
    string ReleaseId,
    string CompositionToolVersion,
    string SourceSetSha256,
    FusionReleaseComposition Composition,
    IReadOnlyList<FusionReleaseSource> Sources,
    IReadOnlyList<FusionReleaseTarget> Targets)
{
    public const int CurrentFormatVersion = 1;
}

internal static class FusionReleaseCompatibility
{
    public static string CompositionToolVersion { get; } =
        GetCompositionToolVersion();

    public static void ValidateCompositionToolVersion(
        FusionReleaseManifest manifest)
    {
        if (!string.Equals(
                manifest.CompositionToolVersion,
                CompositionToolVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Fusion release '{manifest.ReleaseId}' was created with "
                + $"composition tool version '{manifest.CompositionToolVersion}', "
                + $"but apply is running version '{CompositionToolVersion}'.");
        }
    }

    private static string GetCompositionToolVersion()
    {
        var assembly = typeof(GraphQLCompositionSettings).Assembly;
        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? throw new InvalidOperationException(
                "The Fusion composition tool version could not be determined.");
    }
}

internal sealed record FusionReleaseComposition(
    string SettingsSha256,
    FusionReleaseCompositionSettings Settings);

internal sealed record FusionReleaseCompositionSettings(
    DirectiveMergeBehavior? CacheControlMergeBehavior,
    bool? EnableGlobalObjectIdentification,
    NodeResolution? NodeResolution,
    DirectiveMergeBehavior? TagMergeBehavior,
    bool? IncludeSatisfiabilityPaths,
    bool? AllowNonResolvableInterfaceObjects,
    ShareableFieldRuntimeTypeRouting? ShareableFieldRuntimeTypeRouting,
    IReadOnlyList<string> ExcludeByTag)
{
    public static FusionReleaseCompositionSettings From(
        GraphQLCompositionSettings settings)
        => new(
            settings.CacheControlMergeBehavior,
            settings.EnableGlobalObjectIdentification,
            settings.NodeResolution,
            settings.TagMergeBehavior,
            settings.IncludeSatisfiabilityPaths,
            settings.AllowNonResolvableInterfaceObjects,
            settings.ShareableFieldRuntimeTypeRouting,
            settings.ExcludeByTag?
                .Order(StringComparer.Ordinal)
                .ToArray()
                ?? []);

    public GraphQLCompositionSettings ToCompositionSettings(
        string environmentName)
        => new()
        {
            CacheControlMergeBehavior = CacheControlMergeBehavior,
            EnableGlobalObjectIdentification = EnableGlobalObjectIdentification,
            NodeResolution = NodeResolution,
            TagMergeBehavior = TagMergeBehavior,
            IncludeSatisfiabilityPaths = IncludeSatisfiabilityPaths,
            AllowNonResolvableInterfaceObjects = AllowNonResolvableInterfaceObjects,
            ShareableFieldRuntimeTypeRouting = ShareableFieldRuntimeTypeRouting,
            ExcludeByTag = ExcludeByTag.ToHashSet(StringComparer.Ordinal),
            EnvironmentName = environmentName
        };
}

internal sealed record FusionReleaseSource(
    string Name,
    string Version,
    string ArchivePath,
    string ArchiveSha256,
    string ContentSha256);

internal sealed record FusionReleaseTarget(
    string CloudUrl,
    string ApiId,
    string SourceSetSha256,
    IReadOnlyList<FusionReleaseSourceReference> Sources);

internal sealed record FusionReleaseSourceReference(
    string Name,
    string Version,
    string ContentSha256);

internal static class FusionReleaseDigests
{
    public static string ComputeCompositionSha256(
        FusionReleaseCompositionSettings settings)
        => ComputeSha256(JsonSerializer.SerializeToUtf8Bytes(
            settings,
            FusionReleaseStore.SerializerOptions));

    public static string ComputeSourceSetSha256(
        IEnumerable<FusionReleaseSource> sources)
    {
        using var stream = new MemoryStream();

        foreach (var source in sources.OrderBy(
            source => source.Name,
            StringComparer.Ordinal))
        {
            WriteFramed(stream, source.Name);
            WriteFramed(stream, source.Version);
            WriteFramed(stream, source.ContentSha256);
        }

        return ComputeSha256(stream.GetBuffer().AsSpan(0, (int)stream.Length));
    }

    private static void WriteFramed(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(int)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            length,
            bytes.Length);
        stream.Write(length);
        stream.Write(bytes);
    }

    private static string ComputeSha256(ReadOnlySpan<byte> value)
        => Convert.ToHexStringLower(SHA256.HashData(value));
}
