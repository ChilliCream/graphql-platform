using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate;

public interface ISchemaDefinition : INameProvider, IDescriptionProvider, ISyntaxNodeProvider
{
    IObjectTypeDefinition QueryType { get; }

    IObjectTypeDefinition? MutationType { get; }

    IObjectTypeDefinition? SubscriptionType { get; }

    IReadOnlyDirectiveCollection Directives { get; }

    IReadOnlyTypeDefinitionCollection Types { get; }

    IReadOnlyDirectiveDefinitionCollection DirectiveDefinitions { get; }

    IObjectTypeDefinition GetOperationType(OperationType operationType);

    IEnumerable<IObjectTypeDefinition> GetPossibleTypes(ITypeDefinition abstractType);

    IEnumerable<INameProvider> GetAllDefinitions();
}
