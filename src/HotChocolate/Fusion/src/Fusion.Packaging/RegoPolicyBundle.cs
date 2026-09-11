using System.Collections.Immutable;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Describes a manifest-indexed Rego policy bundle (format version 2 and above) to write into a Fusion
/// Archive: one or more policy packages, shared library modules, and an optional root data document.
/// </summary>
public sealed class RegoPolicyBundle
{
    /// <summary>
    /// Gets the policy packages the bundle contains. Each package contributes one or more decisions.
    /// </summary>
    public required ImmutableArray<RegoPolicyBundlePackage> Packages { get; init; }

    /// <summary>
    /// Gets the shared library modules compiled into every policy set the bundle produces.
    /// A library module never becomes a decision.
    /// </summary>
    public ImmutableArray<RegoPolicyBundleModule> Libraries { get; init; } = [];

    /// <summary>
    /// Gets the root data document mounted for the bundle, as UTF-8 encoded JSON, or <c>null</c>
    /// when the bundle carries no data document.
    /// </summary>
    public ReadOnlyMemory<byte>? Data { get; init; }
}

/// <summary>
/// Describes one Rego package folder in a <see cref="RegoPolicyBundle"/>: the package's module files
/// and its shared GraphQL data requirements.
/// </summary>
/// <param name="Package">
/// The Rego package name every module in <paramref name="Modules"/> declares.
/// </param>
/// <param name="Modules">
/// The package's module files. Exactly one module must declare at least one entrypoint decision
/// (a rule annotated with <c># METADATA</c> / <c>entrypoint: true</c>); the remaining modules, if
/// any, are helper modules compiled alongside it but never exposed as decisions.
/// </param>
/// <param name="Requirements">
/// The GraphQL data requirements shared by every decision in the package, as a bare selection set or
/// single fragment definition, or <c>null</c> when the package's decisions read no resource.
/// </param>
public sealed record RegoPolicyBundlePackage(
    string Package,
    ImmutableArray<RegoPolicyBundleModule> Modules,
    ReadOnlyMemory<byte>? Requirements);

/// <summary>
/// A single Rego source module identified by a name unique within its package or library folder.
/// </summary>
/// <param name="Name">The module name, used as the file stem <c>&lt;Name&gt;.rego</c>.</param>
/// <param name="Source">The module source as UTF-8 encoded bytes.</param>
public sealed record RegoPolicyBundleModule(string Name, ReadOnlyMemory<byte> Source);
