using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that the model type condition exists in the schema.
/// </summary>
internal sealed class ModelTypeConditionMustExistInSchemaRule : IOpenApiModelDefinitionValidationRule
{
    public ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiModelDefinition model,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var typeName = model.FragmentDefinition.TypeCondition.Name.Value;

        OpenApiDefinitionValidationResult result;
        if (!context.Schema.Types.TryGetType<IOutputTypeDefinition>(typeName, out _))
        {
            result = OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Type condition '{typeName}' not found in schema.",
                    model));
        }
        else
        {
            result = OpenApiDefinitionValidationResult.Success();
        }

        return ValueTask.FromResult(result);
    }
}
