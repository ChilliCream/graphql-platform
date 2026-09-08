using System.Collections.Immutable;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// The parsed content of a Rego policy bundle's <c>manifest.json</c>: the authoritative index of every
/// policy, library, and data payload the bundle carries, together with the sha256 digest of each.
/// </summary>
internal sealed record RegoPolicyBundleManifest
{
    public required int FormatVersion { get; init; }

    public required ImmutableArray<RegoPolicyBundleManifestPolicy> Policies { get; init; }

    public required ImmutableArray<RegoPolicyBundleManifestFile> Libraries { get; init; }

    public RegoPolicyBundleManifestFile? Data { get; init; }
}

/// <summary>
/// One policy (decision) entry in a Rego policy bundle manifest.
/// </summary>
internal sealed record RegoPolicyBundleManifestPolicy
{
    /// <summary>
    /// The full decision identity, formatted <c>&lt;package&gt;.&lt;rule&gt;</c>.
    /// </summary>
    public required string Name { get; init; }

    public required string Package { get; init; }

    /// <summary>
    /// The Rego entrypoint, formatted <c>data.&lt;package&gt;.&lt;rule&gt;</c>.
    /// </summary>
    public required string Entrypoint { get; init; }

    /// <summary>
    /// The canonical relative paths (from the bundle root) of every module required to compile this
    /// decision.
    /// </summary>
    public required ImmutableArray<string> Modules { get; init; }

    /// <summary>
    /// The canonical relative path of the package's GraphQL data requirements, or <c>null</c> when
    /// the package's decisions read no resource.
    /// </summary>
    public string? Requirements { get; init; }

    /// <summary>
    /// The sha256 digest, formatted <c>sha256:&lt;lowercase hex&gt;</c>, of every path this entry
    /// references (its modules and, when present, its requirements), keyed by that path.
    /// </summary>
    public required ImmutableSortedDictionary<string, string> Sha256 { get; init; }
}

/// <summary>
/// A single hashed payload reference in a Rego policy bundle manifest (a library module or the data
/// document), recording its canonical relative path and sha256 digest.
/// </summary>
internal sealed record RegoPolicyBundleManifestFile
{
    public required string Path { get; init; }

    public required string Sha256 { get; init; }
}
