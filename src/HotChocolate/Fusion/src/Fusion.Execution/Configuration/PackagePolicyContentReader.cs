using System.Buffers;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Reads the policy content of the highest Rego policy format version that this runtime supports from
/// an open <see cref="FusionArchive"/> into a <see cref="PolicyContentSnapshot"/>. An archive may carry
/// policy formats intended for newer runtimes; those are ignored so that this runtime never loads a
/// format it does not understand.
/// </summary>
internal static class PackagePolicyContentReader
{
    private const string RegoLanguage = "rego";

    public static async Task<PolicyContentSnapshot?> ReadAsync(
        FusionArchive archive,
        Version maxFormatVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(maxFormatVersion);

        // GetSupportedRegoPolicyFormats returns the versions in descending order, so the first entry
        // that does not exceed the runtime's maximum supported version is the highest usable format.
        var version = archive.GetSupportedRegoPolicyFormats().FirstOrDefault(v => v <= maxFormatVersion);

        if (version is null)
        {
            return null;
        }

        var manifest = await archive.GetManifestAsync(cancellationToken).ConfigureAwait(false);
        var artifacts = manifest?.Artifacts;
        var policyConfigurations = await archive.GetRegoPoliciesAsync(version, cancellationToken)
            .ConfigureAwait(false);

        var policies = ImmutableArray.CreateBuilder<PolicyContent>(policyConfigurations.Count);

        foreach (var configuration in policyConfigurations)
        {
            byte[] source;
            await using (var stream = await configuration.OpenReadPolicyAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                source = await ReadBytesAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            byte[] requirements;
            await using (var stream = await configuration.OpenReadRequirementsAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                requirements = await ReadBytesAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            var key = $"policies/{RegoLanguage}/{version}/{configuration.Name}";
            var digest = TryGetDigest(artifacts, key)
                ?? ComputeDigest(source, requirements);

            // The interim archive stores one Rego rule per pair keyed by the pair name, so the full
            // policy name is the pair name suffixed with the conventional 'allow' rule and the
            // requirements source projects the resource part.
            policies.Add(new PolicyContent(
                $"{configuration.Name}.allow",
                PolicyContentType.Rego,
                source,
                ParseRequirements(requirements),
                digest));
        }

        var dataOwner = await archive.TryGetRegoDataDocumentAsync(version, cancellationToken)
            .ConfigureAwait(false);

        ReadOnlyMemory<byte> data;
        IDisposable owner;

        if (dataOwner is not null)
        {
            using (dataOwner)
            {
                var span = JsonMarshal.GetRawUtf8Value(dataOwner.Document.RootElement);
                var buffer = new PooledArrayWriter(span.Length);
                buffer.Write(span);
                data = buffer.WrittenMemory;
                owner = buffer;
            }
        }
        else
        {
            var buffer = new PooledArrayWriter(2);
            buffer.Write("{}"u8);
            data = buffer.WrittenMemory;
            owner = buffer;
        }

        var dataDigest = TryGetDigest(artifacts, $"policies/{RegoLanguage}/{version}/data")
            ?? ComputeDigest(data.Span);

        return new PolicyContentSnapshot(
            RegoLanguage,
            version,
            policies.ToImmutable(),
            data,
            dataDigest,
            owner);
    }

    private static PolicyRequirements ParseRequirements(ReadOnlySpan<byte> requirements)
    {
        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(requirements);
        return selectionSet.Selections.Count == 0
            ? PolicyRequirements.Empty
            : new PolicyRequirements { Resource = selectionSet };
    }

    private static byte[]? TryGetDigest(
        ImmutableSortedDictionary<string, string>? artifacts,
        string key)
        => artifacts is not null && artifacts.TryGetValue(key, out var digest)
            ? Encoding.UTF8.GetBytes(digest)
            : null;

    private static async Task<byte[]> ReadBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new PooledArrayWriter();

        int read;
        do
        {
            var memory = buffer.GetMemory(4096);
            read = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);

            if (read > 0)
            {
                buffer.Advance(read);
            }
        } while (read > 0);

        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] ComputeDigest(ReadOnlySpan<byte> source, ReadOnlySpan<byte> requirements)
    {
        var hash = new XxHash64();
        hash.Append(source);
        hash.Append(requirements);
        return Encoding.UTF8.GetBytes(hash.GetCurrentHashAsUInt64().ToString("x16"));
    }

    private static byte[] ComputeDigest(ReadOnlySpan<byte> data)
        => Encoding.UTF8.GetBytes(XxHash64.HashToUInt64(data).ToString("x16"));
}
