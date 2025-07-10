using HotChocolate.Features;
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
    , IFeatureProvider
    , ISyntaxNodeProvider<EnumValueDefinitionNode>
    , ISchemaCoordinateProvider
{
    /// <summary>
    /// Gets the enum type that declares this value.
    /// </summary>
    IEnumTypeDefinition DeclaringType { get; }
}
