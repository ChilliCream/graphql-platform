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
    }
}
