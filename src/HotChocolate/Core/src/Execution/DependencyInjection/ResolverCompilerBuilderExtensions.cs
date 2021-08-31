using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IResolverCompilerBuilder"/>
    /// </summary>
    public static class ResolverCompilerBuilderExtensions
    {
        /// <summary>
        /// Adds a custom parameter compiler to the resolver compiler.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IResolverCompilerBuilder"/>.
        /// </param>
        /// <param name="expression">
        /// A expression that resolves the data for the custom parameter.
        /// </param>
        /// <param name="canHandle">
        /// A predicate that can be used to specify to which parameter the
        /// expression shall be applied to.
        /// </param>
        /// <typeparam name="T">
        /// The parameter result type.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IResolverCompilerBuilder"/> that can be used to configure to
        /// chain in more configuration.
        /// </returns>
        public static IResolverCompilerBuilder AddParameter<T>(
            this IResolverCompilerBuilder builder,
            Expression<Func<IResolverContext, T>> expression,
            Func<ParameterInfo, bool>? canHandle = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (canHandle is null)
            {
                builder.RequestExecutorBuilder.Services.AddParameterExpressionBuilder(
                    _ => new CustomParameterExpressionBuilder<T>(expression));
            }
            else
            {
                builder.RequestExecutorBuilder.Services.AddParameterExpressionBuilder(
                    _ => new CustomParameterExpressionBuilder<T>(expression, canHandle));
            }
            return builder;
        }

        /// <summary>
        /// Marks types as well-known services that no longer need the
        /// <see cref="ServiceAttribute"/> annotation.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IResolverCompilerBuilder"/>.
        /// </param>
        /// <typeparam name="TService">
        /// The well-known service type.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IResolverCompilerBuilder"/> that can be used to configure to
        /// chain in more configuration.
        /// </returns>
        public static IResolverCompilerBuilder AddService<TService>(
            this IResolverCompilerBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.RequestExecutorBuilder.Services
                .TryAddParameterExpressionBuilder<
                    CustomServiceParameterExpressionBuilder<TService>>();
            return builder;
        }
    }
}
