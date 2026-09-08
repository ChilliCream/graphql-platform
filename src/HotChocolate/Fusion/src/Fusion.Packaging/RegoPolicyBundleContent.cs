using System.Collections.Immutable;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// The validated content of a Rego policy bundle read from a Fusion Archive: every module referenced
/// by the bundle manifest has been matched against the archive's actual files, hashed, and cross
/// checked against the manifest's declared identity.
/// </summary>
public sealed class RegoPolicyBundleContent
{
    /// <summary>
    /// Gets the bundle's policy packages, ordered by package name.
    /// </summary>
    public required ImmutableArray<RegoPolicyBundlePackageContent> Packages { get; init; }

    /// <summary>
    /// Gets the bundle's shared library modules, ordered by name. These are compiled into every
    /// policy set exactly once and never become a decision.
    /// </summary>
    public required ImmutableArray<RegoLibraryContent> Libraries { get; init; }

    /// <summary>
    /// Gets the bundle's root data document as UTF-8 encoded JSON, or <c>null</c> when the bundle
    /// carries no data document.
    /// </summary>
    public ReadOnlyMemory<byte>? Data { get; init; }

    /// <summary>
    /// Gets the content digest of the root data document, or the digest of an empty object when the
    /// bundle carries no data document.
    /// </summary>
    public required ReadOnlyMemory<byte> DataDigest { get; init; }
}

/// <summary>
/// The validated content of one Rego policy package: the module that declares its entrypoint
/// decisions, its shared GraphQL data requirements, and a content digest that changes whenever the
/// package's modules or requirements change.
/// </summary>
/// <param name="Package">The Rego package name.</param>
/// <param name="Source">The UTF-8 encoded source of the module that declares the package's decisions.</param>
/// <param name="Requirements">
/// The GraphQL data requirements as UTF-8 encoded bytes, or <c>null</c> when the package's decisions
/// read no resource.
/// </param>
/// <param name="Digest">The content digest of the package, taken from the bundle manifest.</param>
public sealed record RegoPolicyBundlePackageContent(
    string Package,
    ReadOnlyMemory<byte> Source,
    ReadOnlyMemory<byte>? Requirements,
    ReadOnlyMemory<byte> Digest);

/// <summary>
/// A shared Rego module that is compiled alongside every policy set but never becomes a decision:
/// either a bundle-wide library module or a helper module local to one policy package.
/// </summary>
/// <param name="Name">
/// The module's canonical relative path within the bundle (for example <c>lib/rbac.rego</c> or
/// <c>orders/helpers.rego</c>), used as its compiled module name.
/// </param>
/// <param name="Source">The module source as UTF-8 encoded bytes.</param>
/// <param name="Digest">The content digest of the module, taken from the bundle manifest.</param>
public sealed record RegoLibraryContent(string Name, ReadOnlyMemory<byte> Source, ReadOnlyMemory<byte> Digest);
