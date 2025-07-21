namespace HotChocolate.Types;

/// <summary>
/// GraphQL type system members that can be scoped.
/// </summary>
public interface IHasScope
{
    /// <summary>
    /// Gets a scope name provided by an extension.
    /// </summary>
    string? Scope { get; }
}
