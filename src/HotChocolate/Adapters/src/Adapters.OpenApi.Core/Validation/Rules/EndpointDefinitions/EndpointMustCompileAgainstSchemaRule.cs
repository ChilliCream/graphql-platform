using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that after merging endpoint definition with referenced fragment definitions,
/// it must compile against the schema.
/// </summary>
internal sealed class EndpointMustCompileAgainstSchemaRule : IOpenApiEndpointDefinitionValidationRule
{
    public async ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var definitions = new List<IDefinitionNode>
        {
            endpoint.OperationDefinition
        };
        definitions.AddRange(endpoint.LocalFragmentsByName.Values);

        var fragmentReferences = new Queue<string>(endpoint.ExternalFragmentReferences);

        while (fragmentReferences.TryDequeue(out var fragmentName))
        {
            var model = await context.GetModelAsync(fragmentName);

            if (model is not null)
            {
                definitions.Add(model.FragmentDefinition);
                definitions.AddRange(model.LocalFragmentsByName.Values);

                foreach (var fragmentReference in model.ExternalFragmentReferences)
                {
                    fragmentReferences.Enqueue(fragmentReference);
                }
            }
        }

        var documentNode = new DocumentNode(null, definitions.ToArray());

        var validationResult = context.DocumentValidator.Validate(context.Schema, documentNode);

        if (validationResult.HasErrors)
        {
            var firstError = validationResult.Errors
                .Select(error =>
                    new OpenApiDefinitionValidationError(
                        $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' does not compile against the schema: {error.Message}", endpoint))
                .First();

            return OpenApiDefinitionValidationResult.Failure(firstError);
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
