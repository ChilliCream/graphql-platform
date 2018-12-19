using System;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation
{
    public static class QueryExecutionBuilderExtensions
    {
        public static IQueryExecutionBuilder AddQueryValidation(
            this IQueryExecutionBuilder builder)
        {
            builder.Services.AddQueryValidation();
            return builder;
        }

        public static IServiceCollection AddQueryValidation(
            this IServiceCollection services)
        {
            return services.AddSingleton<IQueryValidator, QueryValidator>();
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
            builder.Services.AddSingleton<IQueryValidationRule>(factory);
            return builder;
        }

        public static IServiceCollection AddDefaultValidationRules(
            this IServiceCollection services)
        {
            services.AddSingleton<IQueryValidationRule, ExecutableDefinitionsRule>();
            services.AddSingleton<IQueryValidationRule, LoneAnonymousOperationRule>();
            services.AddSingleton<IQueryValidationRule, OperationNameUniquenessRule>();
            services.AddSingleton<IQueryValidationRule, VariableUniquenessRule>();
            services.AddSingleton<IQueryValidationRule, ArgumentUniquenessRule>();
            services.AddSingleton<IQueryValidationRule, RequiredArgumentRule>();
            services.AddSingleton<IQueryValidationRule, SubscriptionSingleRootFieldRule>();
            services.AddSingleton<IQueryValidationRule, FieldMustBeDefinedRule>();
            services.AddSingleton<IQueryValidationRule, AllVariablesUsedRule>();
            services.AddSingleton<IQueryValidationRule, DirectivesAreInValidLocationsRule>();
            services.AddSingleton<IQueryValidationRule, VariablesAreInputTypesRule>();
            services.AddSingleton<IQueryValidationRule, FieldSelectionMergingRule>();
            services.AddSingleton<IQueryValidationRule, AllVariableUsagesAreAllowedRule>();
            services.AddSingleton<IQueryValidationRule, ArgumentNamesRule>();
            services.AddSingleton<IQueryValidationRule, FragmentsMustBeUsedRule>();
            services.AddSingleton<IQueryValidationRule, FragmentNameUniquenessRule>();
            services.AddSingleton<IQueryValidationRule, LeafFieldSelectionsRule>();
            services.AddSingleton<IQueryValidationRule, FragmentsOnCompositeTypesRule>();
            services.AddSingleton<IQueryValidationRule, FragmentSpreadsMustNotFormCyclesRule>();
            services.AddSingleton<IQueryValidationRule, FragmentSpreadTargetDefinedRule>();
            services.AddSingleton<IQueryValidationRule, FragmentSpreadIsPossibleRule>();
            services.AddSingleton<IQueryValidationRule, FragmentSpreadTypeExistenceRule>();
            services.AddSingleton<IQueryValidationRule, InputObjectFieldNamesRule>();
            services.AddSingleton<IQueryValidationRule, InputObjectRequiredFieldsRule>();
            services.AddSingleton<IQueryValidationRule, InputObjectFieldUniquenessRule>();
            services.AddSingleton<IQueryValidationRule, DirectivesAreDefinedRule>();
            services.AddSingleton<IQueryValidationRule, ValuesOfCorrectTypeRule>();
            return services;
        }
    }
}
