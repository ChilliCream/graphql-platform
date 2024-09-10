#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// GraphQL type system members that have a type identity.
/// </summary>
public interface IHasTypeIdentity
{
    /// <summary>
    /// Gets the type identity of this type system member.
    /// </summary>
    Type? TypeIdentity { get; }
}
