using System;
using HotChocolate.Validation;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ValidationServiceCollectionExtensions
    {
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
