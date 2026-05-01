namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Represents a single <c>@key</c> directive on a federation entity type.
/// </summary>
internal sealed class EntityKeyInfo
{
    /// <summary>
    /// Gets the raw field selection string from <c>@key(fields: "...")</c>.
    /// </summary>
    public required string Fields { get; init; }

    /// <summary>
    /// Gets a value indicating whether the key is resolvable.
    /// Defaults to <c>true</c> when the <c>resolvable</c> argument is omitted.
    /// </summary>
    public bool Resolvable { get; init; } = true;
}
