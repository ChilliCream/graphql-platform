using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateValidationServiceCollectionExtensions
    {
        public static IServiceCollection AddValidation(
            this IServiceCollection services)
        {
            services.TryAddSingleton(sp => new DocumentValidatorContextPool(8));
            services.TryAddSingleton<IDocumentValidator, DocumentValidator>();

            services
                .AddOperationRules()
                .AddDirectiveRules()
                .AddExecutableDefinitionRule()
                .AddFieldRules()
                .AddFragmentRules()
                .AddVariableRules()
                .AddArgumentRules()
                .AddSubscriptionRules();

            return services;
        }
    }
}
