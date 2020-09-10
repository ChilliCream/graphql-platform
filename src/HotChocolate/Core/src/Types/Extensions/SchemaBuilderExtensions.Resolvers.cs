using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

#nullable enable

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
            if (builder is null)
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

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => new ValueTask<object?>(resolver(ctx)));
        }

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => resolver(ctx));
        }

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => new ValueTask<object?>(resolver(ctx)));
        }

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName,
                async ctx => await resolver(ctx).ConfigureAwait(false));
        }

        // Resolver()

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => new ValueTask<object?>(resolver()));
        }

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName, ctx => resolver());
        }

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => new ValueTask<object?>(resolver()));
        }

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(
                builder,
                typeName,
                fieldName,
                async ctx => await resolver().ConfigureAwait(false));
        }

        // Resolver(IResolverContext, CancellationToken)

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => new ValueTask<object?>(resolver(ctx, ctx.RequestAborted)));
        }

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => new ValueTask<object?>(resolver(ctx, ctx.RequestAborted)));
        }

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
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

            return AddResolverInternal(
                builder,
                typeName,
                fieldName,
                async ctx => await resolver(ctx, ctx.RequestAborted));
        }

        // Constant

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
            NameString typeName,
            NameString fieldName,
            object? constantResult)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => new ValueTask<object?>(constantResult));
        }

        public static ISchemaBuilder AddResolver<TResult>(
            this ISchemaBuilder builder,
            NameString typeName,
            NameString fieldName,
            TResult constantResult)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddResolverInternal(builder, typeName, fieldName,
                ctx => new ValueTask<object?>(constantResult));
        }
    }
}
