#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a named GraphQL type.
/// </summary>
public interface INamedType
    : IType
    , IHasName
    , IHasDescription
    , IHasReadOnlyContextData
{
    /// <summary>
    /// Determines whether an instance of a specified type <paramref name="type" />
    /// can be assigned to a variable of the current type.
    /// </summary>
    bool IsAssignableFrom(INamedType type);
}
