using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using static HotChocolate.Stitching.SchemaBuilding.SchemaDirectiveHelper;
using static HotChocolate.Stitching.SchemaBuilding.DirectiveKind;
namespace HotChocolate.Stitching.SchemaBuilding;

internal class DefaultSchemaInspector
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

        ParseTypes(schemaDocument.Definitions, schemaInfo);
        ParseRootTypes(schemaDocument.Definitions, schemaInfo);
        DiscoverFetcher(schemaInfo);

        return schemaInfo;
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
                    schemaInfo.Types[objectTypeInfo.Name] = objectTypeInfo;
                    break;
            }
        }
    }

    private void ParseRootTypes(IReadOnlyList<IDefinitionNode> definition, SchemaInfo schemaInfo)
    {
        foreach (OperationTypeDefinitionNode operation in
            definition.OfType<SchemaDefinitionNode>().Single().OperationTypes)
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
                            objectType.Fetchers.Add(new(schemaInfo.Name, fetchField, arguments));
                        }
                    }
                }
            }
        }
    }
}
