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
            where T : DocumentValidatorVisitor, new() =>
            ConfigureValidation(builder, b => b.TryAddValidationVisitor<T>());

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
            where T : DocumentValidatorVisitor  =>
            ConfigureValidation(builder, b => b.TryAddValidationVisitor(factory));

        public static IRequestExecutorBuilder AddMaxComplexityRule(
            this IRequestExecutorBuilder builder,
            int maxAllowedComplexity) =>
            ConfigureValidation(builder, b => b.AddMaxComplexityRule(maxAllowedComplexity));

        public static IRequestExecutorBuilder AddMaxExecutionDepthRule(
            this IRequestExecutorBuilder builder,
            int maxAllowedExecutionDepth) =>
            ConfigureValidation(builder, b => b.AddMaxExecutionDepthRule(maxAllowedExecutionDepth));

        private static IRequestExecutorBuilder ConfigureValidation(
            IRequestExecutorBuilder builder,
            Action<IValidationBuilder> configure)
        {
            configure(builder.Services.AddValidation(builder.Name));
            return builder;
        }
    }
}
