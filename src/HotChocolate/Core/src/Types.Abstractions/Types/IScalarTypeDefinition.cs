using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IScalarTypeDefinition
    : IOutputTypeDefinition
    , IInputTypeDefinition
    , ISyntaxNodeProvider<ScalarTypeDefinitionNode>
    , ISchemaCoordinateProvider;
