using System;
using HotChocolate;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateValidationServiceCollectionExtensions
    {
        public static IValidationBuilder AddValidation(
            this IServiceCollection services,
            string schemaName = WellKnownSchema.Default)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name is mandatory in order to add validation rules.",
                    nameof(schemaName));
            }

            services.AddOptions();
            services.TryAddSingleton<IValidationConfiguration, ValidationConfiguration>();
            services.TryAddSingleton(sp => new DocumentValidatorContextPool(8));
            services.TryAddSingleton<IDocumentValidatorFactory, DefaultDocumentValidatorFactory>();

            var builder = new DefaultValidationBuilder(schemaName, services);

            builder
                .AddDocumentRules()
                .AddOperationRules()
                .AddFieldRules()
                .AddArgumentRules()
                .AddFragmentRules()
                .AddValueRules()
                .AddDirectiveRules()
                .AddVariableRules();

            return builder;
        }
    }
}
