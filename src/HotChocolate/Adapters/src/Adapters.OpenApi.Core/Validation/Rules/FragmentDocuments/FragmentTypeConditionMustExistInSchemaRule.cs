using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that the fragment type condition exists in the schema.
/// </summary>
internal sealed class FragmentTypeConditionMustExistInSchemaRule : IOpenApiFragmentDocumentValidationRule
{
    public ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiFragmentDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken)
    {
        var typeName = document.FragmentDefinition.TypeCondition.Name.Value;

        OpenApiDocumentValidationResult result;
        if (!context.Schema.Types.TryGetType<IOutputTypeDefinition>(typeName, out _))
        {
            result = OpenApiDocumentValidationResult.Failure(
                new OpenApiDocumentValidationError(
                    $"Type condition '{typeName}' not found in schema.",
                    document));
        }
        else
        {
            result = OpenApiDocumentValidationResult.Success();
        }

        return ValueTask.FromResult(result);
    }
}
