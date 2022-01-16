using HotChocolate;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class HotChocolateValidationServiceCollectionExtensions
{
    public static IServiceCollection AddValidationCore(
        this IServiceCollection services)
    {
        services.AddOptions();
        services.TryAddSingleton<IValidationConfiguration, ValidationConfiguration>();
        services.TryAddSingleton(_ => new DocumentValidatorContextPool());
        services.TryAddSingleton<IDocumentValidatorFactory, DefaultDocumentValidatorFactory>();
        return services;
    }

    public static IValidationBuilder AddValidation(
        this IServiceCollection services,
        NameString schemaName = default)
    {
        schemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

        services.AddValidationCore();

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
