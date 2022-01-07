using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class SchemaInfo
{
    public NameString Name { get; set; } = Schema.DefaultName;

    public ObjectTypeInfo? Query { get; set; }

    public ObjectTypeInfo? Mutation { get; set; }

    public ObjectTypeInfo? Subscription { get; set; }

    public Dictionary<string, ITypeInfo> Types { get; } =
        new Dictionary<string, ITypeInfo>();

    public DocumentNode ToSchemaDocument()
    {
        var definitions = new List<IDefinitionNode>();
        var operations = new List<OperationTypeDefinitionNode>();

        if (Query is not null)
        {
            operations.Add(new OperationTypeDefinitionNode(
                null,
                OperationType.Query,
                new NamedTypeNode(new NameNode(Query.Name.Value))));
        }

        if (Mutation is not null)
        {
            operations.Add(new OperationTypeDefinitionNode(
                null,
                OperationType.Query,
                new NamedTypeNode(new NameNode(Mutation.Name.Value))));
        }

        if (Subscription is not null)
        {
            operations.Add(new OperationTypeDefinitionNode(
                null,
                OperationType.Query,
                new NamedTypeNode(new NameNode(Subscription.Name.Value))));
        }

        definitions.Add(new SchemaDefinitionNode(
            null,
            null,
            Array.Empty<DirectiveNode>(),
            operations));

        if (Query is not null)
        {
            definitions.Add(Query.Definition);
        }

        if (Mutation is not null)
        {
            definitions.Add(Mutation.Definition);
        }

        if (Subscription is not null)
        {
            definitions.Add(Subscription.Definition);
        }

        foreach (ITypeInfo typeInfo in Types.Values)
        {
            definitions.Add(typeInfo.Definition);
        }

        return new DocumentNode(definitions);
    }
}
