using System;
using System.Collections.Generic;
using System.Reflection;
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

            return AddResolverConfigInternal(builder, field, resolver, resultType);
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

        public static ISchemaBuilder AddResolver(
            this ISchemaBuilder builder,
            Type resolverType,
            NameString? typeName = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolverType is null)
            {
                throw new ArgumentNullException(nameof(resolverType));
            }

            if (resolverType is { IsClass: true, IsAbstract: false, IsPublic: true } or
                { IsClass: true, IsAbstract: false, IsNestedPublic: true })
            {
                if (typeName is { IsEmpty: true } or null)
                {
                    typeName = resolverType.IsDefined(typeof(GraphQLNameAttribute))
                        ? resolverType.GetCustomAttribute<GraphQLNameAttribute>()!.Name
                        : resolverType.Name;
                }

                AddResolverTypeInternal(builder, typeName.Value, resolverType);


                return builder;
            }

            throw new ArgumentException(
                "The resolver type needs to be a public non-abstract non-static class.",
                nameof(resolverType));
        }

        public static ISchemaBuilder AddResolver<T>(
            this ISchemaBuilder builder,
            NameString? typeName = null)
            => AddResolver(builder, typeof(T), typeName);

        public static ISchemaBuilder AddRootResolver(this ISchemaBuilder builder, Type resolverType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (resolverType is { IsClass: true } or { IsInterface: true })
            {
                foreach (var property in resolverType.GetProperties())
                {
                    AddResolverTypeInternal(builder, property.Name, property.PropertyType);
                }

                return builder;
            }

            throw new ArgumentException(
                "The resolver type needs to be a class or interface",
                nameof(resolverType));
        }

        public static ISchemaBuilder AddRootResolver<T>(this ISchemaBuilder builder)
            => AddRootResolver(builder, typeof(T));

        public static ISchemaBuilder AddRootResolver<T>(this ISchemaBuilder builder, T root)
            where T : class
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            builder.SetContextData(WellKnownContextData.RootInstance, root);
            return AddRootResolver(builder, typeof(T));
        }

        private static ISchemaBuilder AddResolverInternal(
            ISchemaBuilder builder,
            NameString typeName,
            NameString fieldName,
            FieldResolverDelegate resolver)
        {
            return AddResolverConfigInternal(
                builder,
                new FieldCoordinate(typeName, fieldName),
                resolver,
                null);
        }

        private static ISchemaBuilder AddResolverConfigInternal(
            ISchemaBuilder builder,
            FieldCoordinate field,
            FieldResolverDelegate resolver,
            Type? resultType)
        {
            InitializeResolverTypeInterceptor(builder);

            if (builder.ContextData.TryGetValue(WellKnownContextData.ResolverConfigs, out var o) &&
                o is List<FieldResolverConfig> resolverConfigs)
            {
                resolverConfigs.Add(new(field, resolver, null, resultType));
            }

            return builder;
        }

        private static ISchemaBuilder AddResolverTypeInternal(
            ISchemaBuilder builder,
            NameString typeName,
            Type resolverType)
        {
            InitializeResolverTypeInterceptor(builder);

            if (builder.ContextData.TryGetValue(WellKnownContextData.ResolverTypes, out var o) &&
                o is List<(NameString, Type)> resolverTypes)
            {
                resolverTypes.Add((typeName, resolverType));
            }

            return builder;
        }

        private static ISchemaBuilder BindRuntimeTypeInternal(
            ISchemaBuilder builder,
            NameString typeName,
            Type runtimeType)
        {
            InitializeResolverTypeInterceptor(builder);

            if (builder.ContextData.TryGetValue(WellKnownContextData.RuntimeTypes, out var o) &&
                o is Dictionary<NameString, Type> runtimeTypes)
            {
                runtimeTypes[typeName] = runtimeType;
            }

            return builder;
        }

        private static void InitializeResolverTypeInterceptor(ISchemaBuilder builder)
        {
            if (!builder.ContextData.ContainsKey(WellKnownContextData.ResolverConfigs))
            {
                var resolverConfigs = new List<FieldResolverConfig>();
                var resolverTypes = new List<(NameString, Type)>();
                var runtimeTypes = new Dictionary<NameString, Type>();

                builder.ContextData.Add(WellKnownContextData.ResolverConfigs, resolverConfigs);
                builder.ContextData.Add(WellKnownContextData.ResolverTypes, resolverTypes);
                builder.ContextData.Add(WellKnownContextData.RuntimeTypes, runtimeTypes);

                builder.TryAddTypeInterceptor(
                    new ResolverTypeInterceptor(resolverConfigs, resolverTypes, runtimeTypes));
            }
        }
    }
}
