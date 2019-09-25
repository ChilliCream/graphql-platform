using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        private static ISchemaBuilder AddResolverInternal(
            ISchemaBuilder builder,
            NameString typeName,
            NameString fieldName,
            FieldResolverDelegate resolver)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddResolver(
                new FieldResolver(typeName, fieldName, resolver));
        }

        // AddResolver(IResolverContext)

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => Task.FromResult(resolver(ctx)));
        }

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
            NameString typeName,
            NameString fieldName,
            Func<IResolverContext, Task<object>> resolver)
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

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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
