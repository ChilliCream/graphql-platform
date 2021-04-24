using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Adds a query validation visitor to the schema.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <typeparam name="T">
        /// The type of the validator.
        /// </typeparam>
        /// <returns>
        /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
        /// configuration.
        /// </returns>
        public static IRequestExecutorBuilder AddValidationVisitor<T>(
            this IRequestExecutorBuilder builder)
            where T : DocumentValidatorVisitor, new()
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return ConfigureValidation(builder, b => b.TryAddValidationVisitor<T>());
        }

        /// <summary>
        /// Adds a query validation visitor to the schema.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="factory">
        /// The factory that creates the validator instance.
        /// </param>
        /// <typeparam name="T">
        /// The type of the validator.
        /// </typeparam>
        /// <returns>
        /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
        /// configuration.
        /// </returns>
        public static IRequestExecutorBuilder AddValidationVisitor<T>(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, ValidationOptions, T> factory)
            where T : DocumentValidatorVisitor
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return ConfigureValidation(builder, b => b.TryAddValidationVisitor(factory));
        }

        /// <summary>
        /// Adds a query validation visitor to the schema.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <typeparam name="T">
        /// The type of the validator.
        /// </typeparam>
        /// <returns>
        /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
        /// configuration.
        /// </returns>
        public static IRequestExecutorBuilder AddValidationRule<T>(
            this IRequestExecutorBuilder builder)
            where T : class, IDocumentValidatorRule, new()
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return ConfigureValidation(builder, b => b.TryAddValidationRule<T>());
        }

        /// <summary>
        /// Adds a query validation rule to the schema.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="factory">
        /// The factory that creates the validator instance.
        /// </param>
        /// <typeparam name="T">
        /// The type of the validator.
        /// </typeparam>
        /// <returns>
        /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
        /// configuration.
        /// </returns>
        public static IRequestExecutorBuilder AddValidationRule<T>(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, ValidationOptions, T> factory)
            where T : class, IDocumentValidatorRule
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return ConfigureValidation(builder, b => b.TryAddValidationRule(factory));
        }

        public static IRequestExecutorBuilder AddMaxExecutionDepthRule(
            this IRequestExecutorBuilder builder,
            int maxAllowedExecutionDepth) =>
            ConfigureValidation(builder, b => b.AddMaxExecutionDepthRule(maxAllowedExecutionDepth));

        /// <summary>
        /// Adds a validation rule that only allows requests to use `__schema` or `__type`
        /// if the request carries an introspection allowed flag.
        /// </summary>
        public static IRequestExecutorBuilder AddIntrospectionAllowedRule(
            this IRequestExecutorBuilder builder) =>
            ConfigureValidation(builder, b => b.AddIntrospectionAllowedRule());

        private static IRequestExecutorBuilder ConfigureValidation(
            IRequestExecutorBuilder builder,
            Action<IValidationBuilder> configure)
        {
            configure(builder.Services.AddValidation(builder.Name));
            return builder;
        }
    }
}
