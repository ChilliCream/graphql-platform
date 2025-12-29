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
        new EndpointMustBeQueryOrMutationRule(),
        new EndpointMustHaveSingleRootFieldRule(),
        new EndpointNoDeferStreamDirectiveRule(),
        new EndpointMustHaveValidRouteRule(),
        new EndpointParameterConflictRule()
    ];

    public OpenApiDefinitionValidationResult Validate(IOpenApiDefinition definition)
    {
        if (definition is OpenApiEndpointDefinition endpoint)
        {
            foreach (var rule in s_endpointValidationRules)
            {
                var result = rule.Validate(endpoint);
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
                var result = rule.Validate(model);
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
