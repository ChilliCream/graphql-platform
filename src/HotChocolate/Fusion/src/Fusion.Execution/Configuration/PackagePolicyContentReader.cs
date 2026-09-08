using System.Buffers;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Language;
using ThrowHelper = HotChocolate.Fusion.Execution.ThrowHelper;

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
        var allVersions = archive.GetSupportedRegoPolicyFormats().ToArray();

        if (allVersions.Length == 0)
        {
            // The archive carries no Rego policies of any format. This is distinct from every format
            // present exceeding what this runtime understands, which is rejected below.
            return null;
        }

        var version = allVersions.FirstOrDefault(v => v <= maxFormatVersion);

        if (version is null)
        {
            // Every Rego policy format in the archive is newer than this runtime understands. A
            // runtime built before this check returns null here and silently serves the schema with
            // no policies, which is unsafe for an archive that was intentionally packaged with a
            // newer policy format; that legacy behavior cannot be retrofitted onto archives already
            // deployed, so this runtime instead fails closed and rejects the archive outright.
            throw ThrowHelper.UnsupportedRegoPolicyFormatVersion(allVersions[0], maxFormatVersion);
        }

        if (archive.IsRegoPolicyBundleFormat(version))
        {
            return await ReadBundleAsync(archive, version, cancellationToken).ConfigureAwait(false);
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

            policies.Add(new PolicyContent(
                configuration.Name,
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
            [],
            data,
            dataDigest,
            owner);
    }

    private static async Task<PolicyContentSnapshot> ReadBundleAsync(
        FusionArchive archive,
        Version version,
        CancellationToken cancellationToken)
    {
        var bundle = await archive.GetRegoPolicyBundleAsync(version, cancellationToken).ConfigureAwait(false);

        var policies = ImmutableArray.CreateBuilder<PolicyContent>(bundle.Packages.Length);

        foreach (var package in bundle.Packages)
        {
            var requirements = package.Requirements is { } requirementsBytes
                ? ParseRequirements(requirementsBytes.Span)
                : PolicyRequirements.Empty;

            policies.Add(new PolicyContent(
                package.Package,
                PolicyContentType.Rego,
                package.Source,
                requirements,
                package.Digest));
        }

        var libraries = ImmutableArray.CreateBuilder<PolicyLibraryModule>(bundle.Libraries.Length);

        foreach (var library in bundle.Libraries)
        {
            libraries.Add(new PolicyLibraryModule(library.Name, library.Source, library.Digest));
        }

        return new PolicyContentSnapshot(
            RegoLanguage,
            version,
            policies.ToImmutable(),
            libraries.ToImmutable(),
            bundle.Data ?? "{}"u8.ToArray(),
            bundle.DataDigest,
            dataOwner: null);
    }

    private static PolicyRequirements ParseRequirements(ReadOnlySpan<byte> requirements)
    {
        var selectionSet = HasFragmentRequirementsPrefix(requirements)
            ? ParseFragmentRequirements(requirements)
            : Utf8GraphQLParser.Syntax.ParseSelectionSet(requirements);
        return selectionSet.Selections.Count == 0
            ? PolicyRequirements.Empty
            : new PolicyRequirements { Resource = selectionSet };
    }

    private static SelectionSetNode ParseFragmentRequirements(ReadOnlySpan<byte> requirements)
    {
        var document = Utf8GraphQLParser.Parse(requirements);

        if (document.Definitions.Count != 1
            || document.Definitions[0] is not FragmentDefinitionNode fragment)
        {
            throw ThrowHelper.PolicyRequirementsMustBeSingleFragmentDefinition();
        }

        return fragment.SelectionSet;
    }

    private static bool HasFragmentRequirementsPrefix(ReadOnlySpan<byte> source)
    {
        if (source.Length >= 3 && source[0] == 0xEF && source[1] == 0xBB && source[2] == 0xBF)
        {
            source = source[3..];
        }

        var index = 0;

        while (index < source.Length)
        {
            var current = source[index];

            if (IsGraphQLWhitespace(current) || current == ',')
            {
                index++;
                continue;
            }

            if (current == '#')
            {
                index++;

                while (index < source.Length && source[index] is not (byte)'\r' and not (byte)'\n')
                {
                    index++;
                }

                continue;
            }

            return source[index..].StartsWith("fragment"u8)
                && index + "fragment"u8.Length < source.Length
                && IsGraphQLWhitespace(source[index + "fragment"u8.Length]);
        }

        return false;
    }

    private static bool IsGraphQLWhitespace(byte value)
        => value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';

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
