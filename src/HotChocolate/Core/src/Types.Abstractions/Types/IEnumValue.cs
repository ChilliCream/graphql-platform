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
    , ISyntaxNodeProvider<EnumValueDefinitionNode>
    , ISchemaCoordinateProvider;
