using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation
{
    public static class ValidationQueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder AddComplexityCalculation(
            this IQueryExecutionBuilder builder,
            ComplexityCalculation complexityCalculation)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (complexityCalculation == null)
            {
                throw new ArgumentNullException(nameof(complexityCalculation));
            }

            builder.Services.AddSingleton(
                complexityCalculation);

            return builder;
        }

        public static IQueryExecutionBuilder AddQueryValidation(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddQueryValidation();

            return builder;
        }

        public static IServiceCollection AddQueryValidation(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services
                .AddSingleton<IQueryValidator, QueryValidator>();
        }

        public static IQueryExecutionBuilder AddDefaultValidationRules(
            this IQueryExecutionBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddDefaultValidationRules();

            return builder;
        }

        public static IQueryExecutionBuilder AddValidationRule<T>(
            this IQueryExecutionBuilder builder)
            where T : class, IQueryValidationRule
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IQueryValidationRule, T>();

            return builder;
        }

        public static IQueryExecutionBuilder AddValidationRule(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, IQueryValidationRule> factory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            builder.Services.AddSingleton(factory);

            return builder;
        }

        public static IServiceCollection AddDefaultValidationRules(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services
                .AddSingleton<IQueryValidationRule, ExecutableDefinitionsRule>()
                .AddSingleton<IQueryValidationRule, LoneAnonymousOperationRule>()
                .AddSingleton<IQueryValidationRule, OperationNameUniquenessRule>()
                .AddSingleton<IQueryValidationRule, VariableUniquenessRule>()
                .AddSingleton<IQueryValidationRule, ArgumentUniquenessRule>()
                .AddSingleton<IQueryValidationRule, RequiredArgumentRule>()
                .AddSingleton<IQueryValidationRule, SubscriptionSingleRootFieldRule>()
                .AddSingleton<IQueryValidationRule, FieldMustBeDefinedRule>()
                .AddSingleton<IQueryValidationRule, AllVariablesUsedRule>()
                .AddSingleton<IQueryValidationRule, DirectivesAreInValidLocationsRule>()
                .AddSingleton<IQueryValidationRule, VariablesAreInputTypesRule>()
                .AddSingleton<IQueryValidationRule, FieldSelectionMergingRule>()
                .AddSingleton<IQueryValidationRule, AllVariableUsagesAreAllowedRule>()
                .AddSingleton<IQueryValidationRule, ArgumentNamesRule>()
                .AddSingleton<IQueryValidationRule, FragmentsMustBeUsedRule>()
                .AddSingleton<IQueryValidationRule, FragmentNameUniquenessRule>()
                .AddSingleton<IQueryValidationRule, LeafFieldSelectionsRule>()
                .AddSingleton<IQueryValidationRule, FragmentsOnCompositeTypesRule>()
                .AddSingleton<IQueryValidationRule, FragmentSpreadsMustNotFormCyclesRule>()
                .AddSingleton<IQueryValidationRule, FragmentSpreadTargetDefinedRule>()
                .AddSingleton<IQueryValidationRule, FragmentSpreadIsPossibleRule>()
                .AddSingleton<IQueryValidationRule, FragmentSpreadTypeExistenceRule>()
                .AddSingleton<IQueryValidationRule, InputObjectFieldNamesRule>()
                .AddSingleton<IQueryValidationRule, InputObjectRequiredFieldsRule>()
                .AddSingleton<IQueryValidationRule, InputObjectFieldUniquenessRule>()
                .AddSingleton<IQueryValidationRule, DirectivesAreDefinedRule>()
                .AddSingleton<IQueryValidationRule, ValuesOfCorrectTypeRule>()
                .AddSingleton<IQueryValidationRule, MaxDepthRule>()
                .AddSingleton<IQueryValidationRule>(
                    s => new MaxComplexityRule(
                        s.GetRequiredService<IValidateQueryOptionsAccessor>(),
                        s.GetService<ComplexityCalculation>()));
        }
    }
}
