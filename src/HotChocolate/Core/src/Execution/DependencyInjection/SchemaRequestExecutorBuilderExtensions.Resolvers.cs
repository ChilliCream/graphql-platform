using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
    /// </summary>
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
            Func<IResolverContext, object> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, resolver));
        }

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, ValueTask<object>> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => resolver(ctx));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, TResult> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => Task.FromResult<object>(resolver(ctx)));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, Task<TResult>> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                async ctx =>
                {
                    Task<TResult> resolverTask = resolver(ctx);
                    if (resolverTask == null)
                    {
                        return default;
                    }
                    return await resolverTask.ConfigureAwait(false);
                });
        }

        // Resolver()

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<object> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => Task.FromResult(resolver()));
        }

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<Task<object>> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => resolver());
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<TResult> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => Task.FromResult<object>(resolver()));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<Task<TResult>> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            FieldResolverDelegate resolverDelegate = async ctx =>
            {
                Task<TResult> resolverTask = resolver();
                if (resolverTask == null)
                {
                    return default;
                }
                return await resolverTask.ConfigureAwait(false);
            };

            return AddResolverInternal(
                builder, typeName, fieldName, resolverDelegate);
        }

        // Resolver(IResolverContext, CancellationToken)

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, CancellationToken, object> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => Task.FromResult(resolver(ctx, ctx.RequestAborted)));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, CancellationToken, TResult> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => Task.FromResult<object>(
                    resolver(ctx, ctx.RequestAborted)));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, CancellationToken, Task<TResult>> resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            FieldResolverDelegate resolverDelegate = async ctx =>
            {
                Task<TResult> resolverTask = resolver(
                    ctx, ctx.RequestAborted);
                if (resolverTask == null)
                {
                    return default;
                }
                return await resolverTask.ConfigureAwait(false);
            };

            return AddResolverInternal(
                builder, typeName, fieldName, resolverDelegate);
        }

        // Constant

        public static IRequestExecutorBuilder AddResolver(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            object constantResult)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => Task.FromResult(constantResult));
        }

        public static IRequestExecutorBuilder AddResolver<TResult>(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            TResult constantResult)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => Task.FromResult<object>(constantResult));
        }
    }
}