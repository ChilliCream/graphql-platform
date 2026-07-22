using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates the @responseBody directive on an endpoint definition.
/// </summary>
internal sealed class EndpointSingleResponseBodyDirectiveRule
    : IOpenApiEndpointDefinitionValidationRule
{
    private static readonly ResponseBodyDirectiveFinder s_finder = new();

    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        var finderContext = new ResponseBodyDirectiveFinder.Context();

        s_finder.Visit(endpoint.OperationDefinition, finderContext);

        if (finderContext.Count > 1)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint operations can contain at most one '@responseBody' directive.",
                    endpoint));
        }

        if (finderContext.HasResponseBodyInTypedInlineFragment)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint operations cannot contain the '@responseBody' directive "
                    + "within an inline fragment with a type condition.",
                    endpoint));
        }

        if (HasResponseBodyDirectiveOnNonField(endpoint.OperationDefinition))
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "The '@responseBody' directive can only be applied to fields.",
                    endpoint));
        }

        if (context.Schema is { } schema
            && schema.TryGetOperationType(endpoint.OperationDefinition.Operation, out var rootType)
            && ResponseBodyPathContainsList(
                endpoint.OperationDefinition.SelectionSet,
                rootType,
                false))
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "The path to a field with the '@responseBody' directive cannot contain list fields.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }

    private static bool HasResponseBodyDirectiveOnNonField(
        OperationDefinitionNode operation)
    {
        if (HasResponseBodyDirective(operation.Directives)
            || operation.VariableDefinitions.Any(
                variable => HasResponseBodyDirective(variable.Directives)))
        {
            return true;
        }

        return HasResponseBodyDirectiveOnNonField(operation.SelectionSet);
    }

    private static bool HasResponseBodyDirectiveOnNonField(SelectionSetNode selectionSet)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    if (field.SelectionSet is not null
                        && HasResponseBodyDirectiveOnNonField(field.SelectionSet))
                    {
                        return true;
                    }
                    break;

                case InlineFragmentNode inlineFragment:
                    if (HasResponseBodyDirective(inlineFragment.Directives)
                        || HasResponseBodyDirectiveOnNonField(inlineFragment.SelectionSet))
                    {
                        return true;
                    }
                    break;

                case FragmentSpreadNode fragmentSpread:
                    if (HasResponseBodyDirective(fragmentSpread.Directives))
                    {
                        return true;
                    }
                    break;
            }
        }

        return false;
    }

    private static bool HasResponseBodyDirective(IReadOnlyList<DirectiveNode> directives)
        => directives.Any(d => d.Name.Value == WellKnownDirectiveNames.ResponseBody);

    private static bool ResponseBodyPathContainsList(
        SelectionSetNode selectionSet,
        IOutputType? declaringType,
        bool containsList)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    var fieldType = ResolveFieldType(field, declaringType);

                    if (HasResponseBodyDirective(field.Directives))
                    {
                        return containsList;
                    }

                    if (field.SelectionSet is not null
                        && fieldType is not null
                        && ResponseBodyPathContainsList(
                            field.SelectionSet,
                            fieldType,
                            containsList || fieldType.IsListType()))
                    {
                        return true;
                    }
                    break;

                case InlineFragmentNode { TypeCondition: null } inlineFragment:
                    if (ResponseBodyPathContainsList(
                        inlineFragment.SelectionSet,
                        declaringType,
                        containsList))
                    {
                        return true;
                    }
                    break;
            }
        }

        return false;
    }

    private static IOutputType? ResolveFieldType(FieldNode field, IOutputType? declaringType)
    {
        if (declaringType?.NamedType() is IComplexTypeDefinition complexType
            && complexType.Fields.TryGetField(field.Name.Value, out var fieldDefinition))
        {
            return fieldDefinition.Type;
        }

        return null;
    }
}
