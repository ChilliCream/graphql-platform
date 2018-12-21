using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation
{
    public static class QueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder AddQueryValidation(
            this IQueryExecutionBuilder builder,
            IValidateQueryOptionsAccessor options)
        {
            builder.Services.AddQueryValidation(options);

            return builder;
        }

        public static IServiceCollection AddQueryValidation(
            this IServiceCollection services,
            IValidateQueryOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return services
                .AddSingleton(options)
                .AddSingleton<IQueryValidator, QueryValidator>();
        }

        public static IQueryExecutionBuilder AddDefaultValidationRules(
            this IQueryExecutionBuilder builder)
        {
            builder.Services.AddDefaultValidationRules();

            return builder;
        }

        public static IQueryExecutionBuilder AddValidationRule<T>(
            this IQueryExecutionBuilder builder)
            where T : class, IQueryValidationRule
        {
            builder.Services.AddSingleton<IQueryValidationRule, T>();

            return builder;
        }

        public static IQueryExecutionBuilder AddValidationRule(
            this IQueryExecutionBuilder builder,
            Func<IServiceProvider, IQueryValidationRule> factory)
        {
            builder.Services.AddSingleton(factory);

            return builder;
        }

        public static IServiceCollection AddDefaultValidationRules(
            this IServiceCollection services)
        {
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
                .AddSingleton<IQueryValidationRule, ValuesOfCorrectTypeRule>();
        }
    }
}
