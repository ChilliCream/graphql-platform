using System.Collections.Immutable;
using HotChocolate.Adapters.OpenApi.Validation;

namespace HotChocolate.Adapters.OpenApi;

// TODO: Operation Ids need to be unique, so operation names need to be unique as well
public sealed class OpenApiDefinitionValidator
{
    private static readonly ImmutableArray<IOpenApiModelDefinitionValidationRule> s_modelValidationRules =
    [
        new ModelNameUniquenessRule(),
        new ModelTypeConditionMustExistInSchemaRule(),
        new ModelNoDeferStreamDirectiveRule(),
        new ModelReferencesMustExistRule()
    ];

    private static readonly ImmutableArray<IOpenApiEndpointDefinitionValidationRule> s_endpointValidationRules =
    [
        new EndpointMustBeQueryOrMutationRule(),
        new EndpointMustHaveSingleRootFieldRule(),
        new EndpointNoDeferStreamDirectiveRule(),
        new EndpointMustHaveValidRouteRule(),
        new EndpointParameterConflictRule(),
        new EndpointRouteUniquenessRule(),
        new EndpointModelReferencesMustExistRule(),
        new EndpointMustCompileAgainstSchemaRule()
    ];

    public async ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        IOpenApiDefinition definition,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        if (definition is OpenApiEndpointDefinition endpoint)
        {
            foreach (var rule in s_endpointValidationRules)
            {
                var result = await rule.ValidateAsync(endpoint, context, cancellationToken).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    return result;
                }
            }
        }
        else if (definition is OpenApiModelDefinition model)
        {
            foreach (var rule in s_modelValidationRules)
            {
                var result = await rule.ValidateAsync(model, context, cancellationToken).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    return result;
                }
            }
        }
        else
        {
            throw new NotSupportedException();
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
