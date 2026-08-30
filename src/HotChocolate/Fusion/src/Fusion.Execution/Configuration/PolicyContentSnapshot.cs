using System.Collections.Immutable;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Carries the policy content read from a Fusion package alongside a
/// <see cref="FusionConfiguration"/>. It bundles the policy sources and the merged data document for
/// the highest supported policy format version of a single policy language, together with the content
/// digests that identify each part.
/// </summary>
/// <remarks>
/// The snapshot is read eagerly while the package is open and is owned by the configuration it is
/// carried on. Disposing the configuration disposes the snapshot and releases the memory that backs
/// the merged data document.
/// </remarks>
public sealed class PolicyContentSnapshot : IDisposable
{
    private readonly IDisposable? _dataOwner;

    /// <summary>
    /// Initializes a new instance of <see cref="PolicyContentSnapshot"/>.
    /// </summary>
    /// <param name="language">The policy language the content belongs to.</param>
    /// <param name="formatVersion">The policy format version the content belongs to.</param>
    /// <param name="policies">The policy sources with their content digests.</param>
    /// <param name="data">The merged data document as UTF-8 encoded JSON.</param>
    /// <param name="dataDigest">The content digest of the merged data document.</param>
    /// <param name="dataOwner">The owner of the memory that backs the merged data document.</param>
    public PolicyContentSnapshot(
        string language,
        Version formatVersion,
        ImmutableArray<PolicyContent> policies,
        ReadOnlyMemory<byte> data,
        ReadOnlyMemory<byte> dataDigest,
        IDisposable? dataOwner)
    {
        ArgumentException.ThrowIfNullOrEmpty(language);
        ArgumentNullException.ThrowIfNull(formatVersion);

        Language = language;
        FormatVersion = formatVersion;
        Policies = policies;
        Data = data;
        DataDigest = dataDigest;
        _dataOwner = dataOwner;
    }

    /// <summary>
    /// Gets the policy language the content belongs to.
    /// </summary>
    public string Language { get; }

    /// <summary>
    /// Gets the policy format version the content belongs to.
    /// </summary>
    public Version FormatVersion { get; }

    /// <summary>
    /// Gets the policy sources with their content digests.
    /// </summary>
    public ImmutableArray<PolicyContent> Policies { get; }

    /// <summary>
    /// Gets the merged data document as UTF-8 encoded JSON.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Gets the content digest of the merged data document.
    /// </summary>
    public ReadOnlyMemory<byte> DataDigest { get; }

    /// <summary>
    /// Releases the memory that backs the merged data document.
    /// </summary>
    public void Dispose() => _dataOwner?.Dispose();
}
