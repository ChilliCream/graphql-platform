using System.Collections.Immutable;
using HotChocolate.Adapters.OpenApi.Validation;

namespace HotChocolate.Adapters.OpenApi;

public sealed class OpenApiDefinitionValidator
{
    private static readonly ImmutableArray<IOpenApiModelDefinitionValidationRule> s_modelValidationRules =
    [
        new ModelNoDeferStreamDirectiveRule()
    ];

    private static readonly ImmutableArray<IOpenApiEndpointDefinitionValidationRule> s_endpointValidationRules =
    [
        new EndpointMustHaveNameRule(),
        new EndpointMustBeQueryOrMutationRule(),
        new EndpointMustHaveSingleRootFieldRule(),
        new EndpointNoDeferStreamDirectiveRule(),
        new EndpointHttpMethodMustBeValidRule(),
        new EndpointMustHaveValidRouteRule(),
        new EndpointParametersMustMapCorrectlyRule(),
        new EndpointParametersMustNotConflictRule()
    ];

    public OpenApiDefinitionValidationResult Validate(
        IOpenApiDefinition definition,
        IOpenApiDefinitionValidationContext context)
    {
        if (definition is OpenApiEndpointDefinition endpoint)
        {
            foreach (var rule in s_endpointValidationRules)
            {
                var result = rule.Validate(endpoint, context);
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
                var result = rule.Validate(model, context);
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
