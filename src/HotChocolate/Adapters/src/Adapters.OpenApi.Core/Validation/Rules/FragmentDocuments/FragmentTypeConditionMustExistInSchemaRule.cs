using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that the fragment type condition exists in the schema.
/// </summary>
internal sealed class FragmentTypeConditionMustExistInSchemaRule : IOpenApiFragmentDocumentValidationRule
{
    public ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiFragmentDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        var typeName = document.FragmentDefinition.TypeCondition.Name.Value;

        OpenApiValidationResult result;
        if (!context.Schema.Types.TryGetType<IOutputTypeDefinition>(typeName, out _))
        {
            result = OpenApiValidationResult.Failure(
                new OpenApiValidationError(
                    $"Type condition '{typeName}' not found in schema.",
                    document));
        }
        else
        {
            result = OpenApiValidationResult.Success();
        }

        return ValueTask.FromResult(result);
    }
}
