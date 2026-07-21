using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi;

internal static class OpenApiEndpointDefinitionExtensions
{
    public static OpenApiResponseBodySelection GetResponseBodySelection(
        this OpenApiEndpointDefinition endpoint,
        ISchemaDefinition schema)
    {
        var operation = endpoint.OperationDefinition;
        var rootType = schema.GetOperationType(operation.Operation);
        var rootField = operation.SelectionSet.Selections.FirstOrDefault() as FieldNode
            ?? throw new InvalidOperationException("Expected to have a response field.");
        var defaultResponseBody = new OpenApiResponseBodySelection(
            [rootField.Alias?.Value ?? rootField.Name.Value],
            rootField.SelectionSet,
            ResolveFieldType(rootField, rootType));
        var responseNamePath = new List<string>();

        return FindResponseBody(operation.SelectionSet, rootType) ?? defaultResponseBody;

        OpenApiResponseBodySelection? FindResponseBody(
            SelectionSetNode selectionSet,
            IOutputType? declaringType)
        {
            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field:
                        var fieldType = ResolveFieldType(field, declaringType);
                        responseNamePath.Add(field.Alias?.Value ?? field.Name.Value);

                        if (field.Directives.Any(
                                d => d.Name.Value == WellKnownDirectiveNames.ResponseBody))
                        {
                            return new OpenApiResponseBodySelection(
                                responseNamePath.ToImmutableArray(),
                                field.SelectionSet,
                                fieldType);
                        }

                        if (field.SelectionSet is not null
                            && FindResponseBody(field.SelectionSet, fieldType) is { } nestedResponseBody)
                        {
                            return nestedResponseBody;
                        }

                        responseNamePath.RemoveAt(responseNamePath.Count - 1);
                        break;

                    case InlineFragmentNode inlineFragment:
                        if (FindResponseBody(
                                inlineFragment.SelectionSet,
                                ResolveTypeCondition(inlineFragment.TypeCondition, declaringType))
                            is { } inlineResponseBody)
                        {
                            return inlineResponseBody;
                        }
                        break;
                }
            }

            return null;
        }

        IOutputType? ResolveFieldType(FieldNode field, IOutputType? declaringType)
        {
            if (declaringType?.NamedType() is IComplexTypeDefinition complexType
                && complexType.Fields.TryGetField(field.Name.Value, out var fieldDefinition))
            {
                return fieldDefinition.Type;
            }

            return null;
        }

        IOutputType? ResolveTypeCondition(NamedTypeNode? typeCondition, IOutputType? fallback)
        {
            if (typeCondition is not null
                && schema.Types.TryGetType(typeCondition.Name.Value, out var type)
                && type is IOutputType outputType)
            {
                return outputType;
            }

            return fallback;
        }
    }
}

internal sealed record OpenApiResponseBodySelection(
    ImmutableArray<string> ResponseNamePath,
    SelectionSetNode? SelectionSet,
    IOutputType? FieldType);
