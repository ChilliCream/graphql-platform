using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder ConfigureResolverCompiler(
            this IRequestExecutorBuilder builder,
            Action<IResolverCompilerBuilder> configure)
        {
            configure(new DefaultResolverCompilerBuilder(builder));
            return builder;
        }
    }

    public static class ResolverCompilerBuilderExtensions
    {
        public static IResolverCompilerBuilder AddParameter<T>(
            this IResolverCompilerBuilder builder,
            Expression<Func<IResolverContext, T>> expression,
            Func<ParameterInfo, bool>? canHandle = null)
        {
            if (canHandle is null)
            {
                builder.RequestExecutorBuilder.Services.TryAddParameterExpressionBuilder(
                    _ => new CustomParameterExpressionBuilder<T>(expression));
            }
            else
            {
                builder.RequestExecutorBuilder.Services.TryAddParameterExpressionBuilder(
                    _ => new CustomParameterExpressionBuilder<T>(expression, canHandle));
            }
            return builder;
        }

        public static IResolverCompilerBuilder AddService<TService>(
            this IResolverCompilerBuilder builder)
        {
            builder.RequestExecutorBuilder.Services
                .TryAddParameterExpressionBuilder<
                    CustomServiceParameterExpressionBuilder<TService>>();
            return builder;
        }
    }

    public interface IResolverCompilerBuilder
    {
        IRequestExecutorBuilder RequestExecutorBuilder { get; }
    }

    internal sealed class DefaultResolverCompilerBuilder : IResolverCompilerBuilder
    {
        public DefaultResolverCompilerBuilder(IRequestExecutorBuilder requestExecutorBuilder)
        {
            RequestExecutorBuilder = requestExecutorBuilder;
        }

        public IRequestExecutorBuilder RequestExecutorBuilder { get; }
    }
}
