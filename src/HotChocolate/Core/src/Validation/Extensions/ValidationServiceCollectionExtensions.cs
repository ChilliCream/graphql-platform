using HotChocolate.Validation.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Validation
{
    public static class ValidationServiceCollectionExtensions
    {
        public static IServiceCollection AddValidation(this IServiceCollection services)
        {
            services.TryAddSingleton<DocumentValidationContextPool>();
            services.AddAllVariablesUsedRule();
            return services;
        }

        public static IServiceCollection AddAllVariablesUsedRule(this IServiceCollection services)
        {
            return services.AddValidationRule<AllVariablesUsedVisitor>();
        }

        public static IServiceCollection AddValidationRule<T>(this IServiceCollection services)
            where T : DocumentValidationVisitor, new()
        {
            return services.AddSingleton<IDocumentValidationRule, DocumentValidationRule<T>>();
        }
    }
}
