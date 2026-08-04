using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace HotChocolate.Fusion;

/// <summary>
/// Options that control how the URLs of source schemas are resolved while the source schema
/// settings are composed into gateway settings.
/// </summary>
internal sealed record SettingsComposerOptions
{
    /// <summary>
    /// Gets the options that compose the configured source schema URLs as they are, without local
    /// overrides and without a preference for development URLs.
    /// </summary>
    public static SettingsComposerOptions Default { get; } = new();

    /// <summary>
    /// Gets the HTTP transport URLs that replace the configured URLs, keyed by source schema name.
    /// The configured <c>url</c> and <c>devUrl</c> of a source schema in this map are ignored.
    /// </summary>
    public IReadOnlyDictionary<string, string> LocalUrlOverrides { get; init; } =
        ReadOnlyDictionary<string, string>.Empty;

    /// <summary>
    /// Gets a value indicating whether the <c>devUrl</c> of a source schema that has no local URL
    /// override takes precedence over its <c>url</c>.
    /// </summary>
    public bool PreferDevUrls { get; init; }

    /// <summary>
    /// Gets the names of the source schemas that belong to the local development environment.
    /// Their settings resolve against the environment that is passed to
    /// <see cref="SettingsComposer.Compose"/>, every other source schema resolves against
    /// <see cref="ExternalEnvironment"/>.
    /// </summary>
    public IReadOnlySet<string> LocalSourceSchemas { get; init; } = FrozenSet<string>.Empty;

    /// <summary>
    /// Gets the environment that the settings of a source schema which is not listed in
    /// <see cref="LocalSourceSchemas"/> resolve against. When it is <c>null</c>, every source
    /// schema resolves against the environment that is passed to
    /// <see cref="SettingsComposer.Compose"/>.
    /// </summary>
    public string? ExternalEnvironment { get; init; }
}
