using HotChocolate.Validation.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Validation
{
    public static class ValidationServiceCollectionExtensions
    {
        public static IServiceCollection AddValidation(
            this IServiceCollection services)
        {
            services.TryAddSingleton(sp => new DocumentValidatorContextPool(8));
            services.TryAddSingleton<IDocumentValidator, DocumentValidator>();
            services.AddAllVariablesUsedRule();
            return services;
        }

        public static IServiceCollection AddAllVariablesUsedRule(
            this IServiceCollection services)
        {
            return services.AddValidationRule<AllVariablesUsedVisitor>();
        }

        public static IServiceCollection AddAllVariableUsagesAreAllowedRule(
            this IServiceCollection services)
        {
            return services.AddValidationRule<AllVariableUsagesAreAllowedVisitor>();
        }

        public static IServiceCollection AddValidationRule<T>(
            this IServiceCollection services)
            where T : DocumentValidatorVisitor, new()
        {
            return services.AddSingleton<IDocumentValidatorRule, DocumentValidatorRule<T>>();
        }
    }
}
