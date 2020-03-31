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

            services
                .AddAllVariablesUsedRule()
                .AddAllVariableUsagesAreAllowedRule()
                .AddDirectivesRule()
                .AddExecutableDefinitionsRule()
                .AddVariableUniqueAndInputTypeRule();

            return services;
        }

        /// <summary>
        /// All variables defined by an operation must be used in that operation
        /// or a fragment transitively included by that operation.
        ///
        /// Unused variables cause a validation error.
        ///
        /// http://spec.graphql.org/June2018/#sec-All-Variables-Used
        ///
        /// AND
        ///
        /// Variables are scoped on a per‐operation basis. That means that
        /// any variable used within the context of an operation must be defined
        /// at the top level of that operation
        ///
        /// http://spec.graphql.org/June2018/#sec-All-Variable-Uses-Defined
        /// </summary>
        public static IServiceCollection AddAllVariablesUsedRule(
            this IServiceCollection services)
        {
            return services.AddValidationRule<AllVariablesUsedVisitor>();
        }

        /// <summary>
        /// Variable usages must be compatible with the arguments
        /// they are passed to.
        ///
        /// Validation failures occur when variables are used in the context
        /// of types that are complete mismatches, or if a nullable type in a
        ///  variable is passed to a non‐null argument type.
        ///
        /// http://spec.graphql.org/June2018/#sec-All-Variable-Usages-are-Allowed
        /// </summary>
        public static IServiceCollection AddAllVariableUsagesAreAllowedRule(
            this IServiceCollection services)
        {
            return services.AddValidationRule<AllVariableUsagesAreAllowedVisitor>();
        }

        /// <summary>
        /// GraphQL servers define what directives they support.
        /// For each usage of a directive, the directive must be available
        /// on that server.
        ///
        /// http://spec.graphql.org/June2018/#sec-Directives-Are-Defined
        ///
        /// AND
        ///
        /// GraphQL servers define what directives they support and where they
        /// support them.
        ///
        /// For each usage of a directive, the directive must be used in a
        /// location that the server has declared support for.
        ///
        /// http://spec.graphql.org/June2018/#sec-Directives-Are-In-Valid-Locations
        ///
        /// AND
        ///
        /// Directives are used to describe some metadata or behavioral change on
        /// the definition they apply to.
        ///
        /// When more than one directive of the
        /// same name is used, the expected metadata or behavior becomes ambiguous,
        /// therefore only one of each directive is allowed per location.
        ///
        /// http://spec.graphql.org/draft/#sec-Directives-Are-Unique-Per-Location
        /// </summary>
        public static IServiceCollection AddDirectivesRule(
            this IServiceCollection services)
        {
            return services.AddValidationRule<DirectivesVisitor>();
        }

        /// <summary>
        /// GraphQL execution will only consider the executable definitions
        /// Operation and Fragment.
        ///
        /// Type system definitions and extensions are not executable,
        /// and are not considered during execution.
        ///
        /// To avoid ambiguity, a document containing TypeSystemDefinition
        /// is invalid for execution.
        ///
        /// GraphQL documents not intended to be directly executed may
        /// include TypeSystemDefinition.
        ///
        /// http://spec.graphql.org/June2018/#sec-Executable-Definitions
        /// </summary>
        public static IServiceCollection AddExecutableDefinitionsRule(
            this IServiceCollection services)
        {
            return services.AddSingleton<IDocumentValidatorRule, ExecutableDefinitionsRule>();
        }

        public static IServiceCollection AddValidationRule<T>(
            this IServiceCollection services)
            where T : DocumentValidatorVisitor, new()
        {
            return services.AddSingleton<IDocumentValidatorRule, DocumentValidatorRule<T>>();
        }

        /// <summary> 
        /// If any operation defines more than one variable with the same name,
        /// it is ambiguous and invalid. It is invalid even if the type of the
        /// duplicate variable is the same.
        ///
        /// http://spec.graphql.org/June2018/#sec-Validation.Variables
        /// 
        /// AND
        /// 
        /// Variables can only be input types. Objects,
        /// unions, and interfaces cannot be used as inputs.
        ///
        /// http://spec.graphql.org/June2018/#sec-Variables-Are-Input-Types
        /// </summary>
        public static IServiceCollection AddVariableUniqueAndInputTypeRule(
            this IServiceCollection services)
        {
            return services.AddValidationRule<VariableUniqueAndInputTypeVisitor>();
        }
    }
}
