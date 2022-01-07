using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

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
}

internal static class SchemaDirectiveHelper
{
    public static IList<ISchemaBuildingDirective> ParseDirectives(IHasDirectives syntaxNode)
    {
        var directives = new List<ISchemaBuildingDirective>();

        foreach (DirectiveNode directive in syntaxNode.Directives)
        {
            if (IsDirective.TryParse(directive, out var isDirective))
            {
                directives.Add(isDirective);
                continue;
            }
        }

        return directives;
    }
}

internal interface ISchemaBuildingDirective
{
    DirectiveKind Kind { get; }
}

public enum DirectiveKind
{
    Is
}

internal readonly struct IsDirective : ISchemaBuildingDirective
{
    public IsDirective(SchemaCoordinate coordinate)
    {
        Coordinate = coordinate;
    }

    public DirectiveKind Kind => DirectiveKind.Is;

    public SchemaCoordinate Coordinate { get; }

    public static bool TryParse(DirectiveNode directiveSyntax, out IsDirective directive)
    {
        if (directiveSyntax is null)
        {
            throw new ArgumentNullException(nameof(directiveSyntax));
        }

        if (directiveSyntax.Name.Value.EqualsOrdinal("is") &&
            directiveSyntax.Arguments.Count is 1)
        {
            ArgumentNode argument = directiveSyntax.Arguments[0];
            if (argument.Name.Value.EqualsOrdinal("a") &&
                argument.Value.Kind is SyntaxKind.StringValue &&
                SchemaCoordinate.TryParse((string)argument.Value.Value!, out var coordinate))
            {
                directive = new IsDirective(coordinate.Value);
                return true;
            }
        }

        directive = default;
        return false;
    }
}