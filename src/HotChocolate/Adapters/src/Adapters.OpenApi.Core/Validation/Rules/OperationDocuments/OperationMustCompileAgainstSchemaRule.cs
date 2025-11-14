using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

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
        var definitions = new List<IDefinitionNode>
        {
            document.OperationDefinition
        };
        definitions.AddRange(document.LocalFragmentLookup.Values);

        var fragmentReferences = new Queue<string>(document.ExternalFragmentReferences);

        while (fragmentReferences.TryDequeue(out var fragmentName))
        {
            var fragment = await context.GetFragmentAsync(fragmentName);

            if (fragment is not null)
            {
                definitions.Add(fragment.FragmentDefinition);
                definitions.AddRange(fragment.LocalFragmentLookup.Values);

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
            var firstError = validationResult.Errors
                .Select(error =>
                    new OpenApiValidationError(
                        $"Operation '{document.Name}' does not compile against the schema: {error.Message}", document))
                .First();

            return OpenApiValidationResult.Failure(firstError);
        }

        return OpenApiValidationResult.Success();
    }
}
