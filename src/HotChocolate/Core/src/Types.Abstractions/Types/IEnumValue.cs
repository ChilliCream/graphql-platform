using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Represents a possible value of an <see cref="IEnumTypeDefinition"/>.
/// </summary>
public interface IEnumValue
    : INameProvider
    , IDirectivesProvider
    , IDescriptionProvider
    , IDeprecationProvider
    , ISyntaxNodeProvider
{
    /// <summary>
    /// Creates an <see cref="EnumValueNode"/> from the current <see cref="IEnumValue"/>.
    /// </summary>
    /// <returns>
    /// Returns an <see cref="EnumValueNode"/>.
    /// </returns>
    new EnumValueNode ToSyntaxNode();
}
