using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Interceptors;

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
            return AddResolverConfig(
                builder,
                new FieldCoordinate(typeName, fieldName),
                resolver,
                null);
        }

        private static ISchemaBuilder AddResolverConfig(
            ISchemaBuilder builder,
            FieldCoordinate field,
            FieldResolverDelegate resolver,
            Type? resultType)
        {
            List<FieldResolverConfig>? configs = null;

            builder.SetContextData(
                nameof(FieldResolverConfig),
                current =>
                {
                    if (current is null)
                    {
                        configs = new() { new(field, resolver, null, resultType) };
                        return configs;
                    }

                    if (current is List<FieldResolverConfig> list)
                    {
                        list.Add(new(field, resolver, null, resultType));
                        return list;
                    }

                    throw new NotSupportedException(
                        TypeResources.SchemaBuilderExtensions_AddResolverConfig_ContextInvalid);
                });

            if (configs is not null)
            {
                builder.TryAddTypeInterceptor(new ResolverTypeInterceptor(configs));
            }

            return builder;
        }

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
            FieldCoordinate field,
            FieldResolverDelegate resolver,
            Type? resultType = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (!field.HasValue)
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilderExtensions_AddResolver_EmptyCooridnates,
                    nameof(builder));
            }

            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return AddResolverConfig(builder, field, resolver, resultType);
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
                _ => new ValueTask<object?>(resolver()));
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

            return AddResolverInternal(builder, typeName, fieldName, _ => resolver());
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
                _ => new ValueTask<object?>(resolver()));
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
                async _ => await resolver().ConfigureAwait(false));
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
                async ctx => await resolver(ctx, ctx.RequestAborted).ConfigureAwait(false));
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
                _ => new ValueTask<object?>(constantResult));
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
                _ => new ValueTask<object?>(constantResult));
        }
    }
}
