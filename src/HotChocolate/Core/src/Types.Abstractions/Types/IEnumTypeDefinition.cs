using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL enum type definition.
/// </summary>
public interface IEnumTypeDefinition : ITypeDefinition
{
    /// <summary>
    /// Gets all possible values if this type.
    /// </summary>
    IReadOnlyEnumValueCollection Values { get; }

    /// <summary>
    /// Tries to get the <paramref name="value"/> for
    /// the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <param name="value">
    /// The GraphQL enum value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the <paramref name="name"/> represents a value of this enum type;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value);
}
