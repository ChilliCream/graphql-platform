using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate;

public interface ISchemaDefinition
{
    public IObjectTypeDefinition? QueryType { get; }

    public IObjectTypeDefinition? MutationType { get; }

    public IObjectTypeDefinition? SubscriptionType { get; }

    public IReadOnlyDirectiveCollection Directives { get; }

    public IReadOnlyTypeDefinitionCollection Types { get; }

    public IReadOnlyDirectiveDefinitionCollection DirectiveDefinitions { get; }

    public IObjectTypeDefinition GetOperationType(OperationType operationType);
}
