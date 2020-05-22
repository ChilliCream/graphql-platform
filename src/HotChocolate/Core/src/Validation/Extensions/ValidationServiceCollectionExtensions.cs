using System;
using HotChocolate;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;
using HotChocolate.Validation.Properties;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateValidationServiceCollectionExtensions
    {
        public static IValidationBuilder AddValidation(
            this IServiceCollection services,
            string? schemaName = null)
        {
            services.AddOptions();
            services.TryAddSingleton<IValidationConfiguration, ValidationConfiguration>();
            services.TryAddSingleton(sp => new DocumentValidatorContextPool(8));
            services.TryAddSingleton<IDocumentValidatorFactory, DefaultDocumentValidatorFactory>();

            var builder = new DefaultValidationBuilder(
                schemaName ?? Options.Options.DefaultName, 
                services);

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
