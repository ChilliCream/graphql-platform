using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            FieldResolver fieldResolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (fieldResolver is null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(fieldResolver));
        }

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, object?> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, ValueTask<object?>> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, TResult> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, ValueTask<TResult>> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        // Resolver()

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<object?> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<ValueTask<object?>> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<TResult> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<ValueTask<TResult>> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        // Resolver(IResolverContext, CancellationToken)

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, CancellationToken, object?> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, CancellationToken, TResult> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, CancellationToken, ValueTask<TResult>> resolver)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        // Constant

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            object? constantValue)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, constantValue));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            TResult constantValue)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, constantValue));
        }
    }
}