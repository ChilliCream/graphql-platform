using HotChocolate.Language;

namespace HotChocolate.Exporters.OpenApi.Validation;

/// <summary>
/// Validates that after merging operation document with referenced fragment definitions,
/// it must compile against the schema.
/// </summary>
internal sealed class OperationMustCompileAgainstSchemaRule : IOpenApiOperationDocumentValidationRule
{
    public async ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        var definitions = new List<IDefinitionNode>();
        definitions.Add(document.OperationDefinition);
        definitions.AddRange(document.LocalFragmentLookup.Values);

        var fragmentReferences = new Queue<string>(document.ExternalFragmentReferences);

        while (fragmentReferences.TryDequeue(out var fragmentName))
        {
            var fragment = await context.GetFragmentAsync(fragmentName);

            if (fragment is not null)
            {
                definitions.Add(fragment.FragmentDefinition);

                foreach (var localFragment in fragment.LocalFragmentLookup.Values)
                {
                    definitions.Add(localFragment);
                }

                foreach (var fragmentReference in fragment.ExternalFragmentReferences)
                {
                    fragmentReferences.Enqueue(fragmentReference);
                }
            }
        }

        var documentNode = new DocumentNode(null, definitions.ToArray());

        var validationResult = context.DocumentValidator.Validate(context.Schema, documentNode);

        if (validationResult.HasErrors)
        {
            var errors = validationResult.Errors.Select(error =>
                new OpenApiValidationError(
                    $"Operation '{document.Name}' does not compile against the schema: {error.Message}",
                    document.Id,
                    document.Name)).ToList();

            return OpenApiValidationResult.Failure(errors);
        }

        return OpenApiValidationResult.Success();
    }
}
