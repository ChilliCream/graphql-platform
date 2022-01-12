using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class SchemaInspector
{
    public SchemaInfo Inspect(DocumentNode schemaDocument)
    {
        if (schemaDocument is null)
        {
            throw new ArgumentNullException(nameof(schemaDocument));
        }

        if (schemaDocument.Definitions.Any(t => t is IExecutable))
        {
            throw new ArgumentException(
                "Only schema documents are allowed.",
                nameof(schemaDocument));
        }

        var schemaInfo = new SchemaInfo();

        SchemaDefinitionNode schemaDefinition = ParseSchema(schemaDocument.Definitions, schemaInfo);
        ParseTypes(schemaDocument.Definitions, schemaInfo);
        ParseRootTypes(schemaDefinition, schemaInfo);
        DiscoverFetcher(schemaInfo);

        return schemaInfo;
    }

    private SchemaDefinitionNode ParseSchema(
        IReadOnlyList<IDefinitionNode> definition,
        SchemaInfo schemaInfo)
    {
        SchemaDefinitionNode schemaDefinition = definition.OfType<SchemaDefinitionNode>().Single();

        schemaInfo.Name =
            SchemaDirective.TryParseFirst(schemaDefinition, out var schemaDirective)
                ? schemaDirective.Name
                : Schema.DefaultName;

        return schemaDefinition;
    }

    private void ParseTypes(IReadOnlyList<IDefinitionNode> definition, SchemaInfo schemaInfo)
    {
        foreach (IDefinitionNode node in definition)
        {
            switch (node.Kind)
            {
                case SyntaxKind.ObjectTypeDefinition:
                    var objectType = (ObjectTypeDefinitionNode)node;
                    var objectTypeInfo = new ObjectTypeInfo(objectType);
                    objectTypeInfo.Bindings.Add(
                        new FieldSchemaBinding(
                            schemaInfo.Name,
                            objectType.Fields.Select(t => t.Name.Value).ToArray()));
                    schemaInfo.Types[objectTypeInfo.Name] = objectTypeInfo;
                    break;
            }
        }
    }

    private void ParseRootTypes(SchemaDefinitionNode schemaDefinition, SchemaInfo schemaInfo)
    {
        foreach (OperationTypeDefinitionNode operation in schemaDefinition.OperationTypes)
        {
            if (schemaInfo.Types.TryGetValue(operation.Type.Name.Value, out ITypeInfo? typeInfo) &&
                typeInfo is ObjectTypeInfo rootType)
            {
                schemaInfo.Types.Remove(typeInfo.Name);

                switch (operation.Operation)
                {
                    case OperationType.Query:
                        schemaInfo.Query = rootType;
                        break;

                    case OperationType.Mutation:
                        schemaInfo.Mutation = rootType;
                        break;

                    case OperationType.Subscription:
                        schemaInfo.Subscription = rootType;
                        break;
                }
            }
        }
    }

    private void DiscoverFetcher(SchemaInfo schemaInfo)
    {
        if (schemaInfo.Query is not null)
        {
            var fieldLookup = schemaInfo.Query.Definition.Fields.ToLookup(t => t.Type.NamedType().Name.Value);

            foreach (ObjectTypeInfo objectType in schemaInfo.Types.Values.OfType<ObjectTypeInfo>())
            {
                foreach (FieldDefinitionNode fetchField in fieldLookup[objectType.Name.Value])
                {
                    if (fetchField.Arguments.Count is not 0)
                    {
                        var arguments = new List<ArgumentInfo>();

                        foreach (InputValueDefinitionNode argument in fetchField.Arguments)
                        {
                            if (!IsDirective.TryParseFirst(argument, out var directive))
                            {
                                break;
                            }

                            arguments.Add(new(
                                argument.Name.Value,
                                argument.Type,
                                directive.Coordinate));
                        }

                        if (arguments.Count == fetchField.Arguments.Count)
                        {
                            var fieldNode = new FieldNode(
                                null, 
                                fetchField.Name, 
                                null, 
                                null, 
                                Array.Empty<DirectiveNode>(), 
                                Array.Empty<ArgumentNode>(), 
                                null);

                            objectType.Fetchers.Add(new(schemaInfo.Name, fieldNode, arguments));
                        }
                    }
                }
            }
        }
    }
}
