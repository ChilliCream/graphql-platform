using System;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IValidationBuilder"/>
    /// </summary>
    public static partial class HotChocolateValidationBuilderExtensions
    {
        /// <summary>
        /// Configures the GraphQL request validation that will be used to validate
        /// incoming GraphQL requests.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IValidationBuilder"/>.
        /// </param>
        /// <param name="configure">
        /// A delegate that is used to configure the <see cref="ValidationOptions"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IValidationBuilder"/> that can be used to configure
        /// the GraphQL request validation.
        /// </returns>
        public static IValidationBuilder ConfigureValidation(
            this IValidationBuilder builder,
            Action<ValidationOptionsModifiers> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure(builder.Name, configure);

            return builder;
        }

        /// <summary>
        /// Configures the GraphQL request validation that will be used to validate
        /// incoming GraphQL requests.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure the <see cref="ValidationOptions"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IValidationBuilder"/> that can be used to configure
        /// the GraphQL request validation.
        /// </returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureClient"/>
        /// will be the application's root service provider instance.
        /// </remarks>
        public static IValidationBuilder ConfigureValidation(
            this IValidationBuilder builder,
            Action<IServiceProvider, ValidationOptionsModifiers> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.AddTransient<IConfigureOptions<ValidationOptionsModifiers>>(sp =>
                new ConfigureNamedOptions<ValidationOptionsModifiers>(
                    builder.Name,
                    modifier => configureClient(sp, modifier)));

            return builder;
        }

        internal static IValidationBuilder UseMultipliers(
            this IValidationBuilder builder, bool useMultipliers) =>
            builder.ConfigureValidation(m =>
                m.Modifiers.Add(o => o.UseComplexityMultipliers = useMultipliers));

        internal static IValidationBuilder SetAllowedComplexity(
            this IValidationBuilder builder, int allowedComplexity) =>
            builder.ConfigureValidation(m =>
                m.Modifiers.Add(o => o.MaxAllowedComplexity = allowedComplexity));

        internal static IValidationBuilder SetAllowedExecutionDepth(
            this IValidationBuilder builder, int allowedExecutionDepth) =>
            builder.ConfigureValidation(m =>
                m.Modifiers.Add(o => o.MaxAllowedExecutionDepth = allowedExecutionDepth));

        public static IValidationBuilder SetComplexityCalculation(
            this IValidationBuilder builder, ComplexityCalculation calculation) =>
            builder.ConfigureValidation(m =>
                m.Modifiers.Add(o => o.ComplexityCalculation = calculation));

        public static IValidationBuilder AddValidationRule<T>(
            this IValidationBuilder builder)
            where T : DocumentValidatorVisitor, new()
        {
            return builder.ConfigureValidation(m =>
                m.Modifiers.Add(o => o.Rules.Add(new DocumentValidatorRule<T>(new T()))));
        }

        public static IValidationBuilder AddValidationRule<T>(
            this IValidationBuilder builder,
            Func<IServiceProvider, ValidationOptions, T> factory)
            where T : DocumentValidatorVisitor
        {
            return builder.ConfigureValidation((s, m) =>
                m.Modifiers.Add(o => o.Rules.Add(new DocumentValidatorRule<T>(factory(s, o)))));
        }
    }
}
