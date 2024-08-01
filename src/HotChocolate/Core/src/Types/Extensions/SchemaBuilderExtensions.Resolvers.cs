using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Interceptors;

#nullable enable

namespace HotChocolate;

public static partial class SchemaBuilderExtensions
{
    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="fieldCoordinate">
    /// The schema coordinate of the field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <param name="resultType">
    /// The resolver result type.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver(
        this ISchemaBuilder builder,
        SchemaCoordinate fieldCoordinate,
        FieldResolverDelegate resolver,
        Type? resultType = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        return AddResolverConfigInternal(builder, fieldCoordinate, resolver, resultType);
    }

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

        return AddResolverInternal(
            builder,
            typeName,
            fieldName,
            ctx => new ValueTask<object?>(resolver(ctx)));
    }

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver<TResult>(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver<TResult>(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver<TResult>(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver<TResult>(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver<TResult>(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="resolver">
    /// The resolver delegate.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver<TResult>(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
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

    /// <summary>
    /// Adds a resolver delegate that returns a constant result.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="constantResult">
    /// The constant result that will be returned for the specified field.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
        object? constantResult)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return AddResolverInternal(builder, typeName, fieldName,
            _ => new ValueTask<object?>(constantResult));
    }

    /// <summary>
    /// Adds a resolver delegate that returns a constant result.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <param name="fieldName">
    /// The field to which the resolver is bound.
    /// </param>
    /// <param name="constantResult">
    /// The constant result that will be returned for the specified field.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver<TResult>(
        this ISchemaBuilder builder,
        string typeName,
        string fieldName,
        TResult constantResult)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return AddResolverInternal(builder, typeName, fieldName,
            _ => new ValueTask<object?>(constantResult));
    }

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="resolverType">
    /// The type that holds one or many resolvers
    /// for the specified <paramref name="typeName"/>.
    /// </param>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver(
        this ISchemaBuilder builder,
        Type resolverType,
        string? typeName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (resolverType is null)
        {
            throw new ArgumentNullException(nameof(resolverType));
        }

        if (resolverType is { IsClass: true, IsAbstract: false, IsPublic: true, } or
            { IsClass: true, IsAbstract: false, IsNestedPublic: true, })
        {
            if (string.IsNullOrEmpty(typeName))
            {
                typeName = resolverType.IsDefined(typeof(GraphQLNameAttribute))
                    ? resolverType.GetCustomAttribute<GraphQLNameAttribute>()!.Name
                    : resolverType.Name;
            }

            AddResolverTypeInternal(builder, typeName, resolverType);

            return builder;
        }

        throw new ArgumentException(
            TypeResources.SchemaBuilderExtensions_AddResolver_TypeConditionNotMet,
            nameof(resolverType));
    }

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type that holds one or many resolvers
    /// for the specified <paramref name="typeName"/>.
    /// </typeparam>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddResolver<T>(
        this ISchemaBuilder builder,
        string? typeName = null)
        => AddResolver(builder, typeof(T), typeName);

    public static ISchemaBuilder AddRootResolver(this ISchemaBuilder builder, Type resolverType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (resolverType is { IsClass: true, } or { IsInterface: true, })
        {
            foreach (var property in resolverType.GetProperties())
            {
                AddResolverTypeInternal(builder, property.Name, property.PropertyType);
            }

            return builder;
        }

        throw new ArgumentException(
            TypeResources.SchemaBuilderExtensions_AddRootResolver_NeedsToBeClassOrInterface,
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
        string typeName,
        string fieldName,
        FieldResolverDelegate resolver)
    {
        return AddResolverConfigInternal(
            builder,
            new SchemaCoordinate(typeName, fieldName),
            resolver,
            null);
    }

    private static ISchemaBuilder AddResolverConfigInternal(
        ISchemaBuilder builder,
        SchemaCoordinate fieldCoordinate,
        FieldResolverDelegate resolver,
        Type? resultType)
    {
        InitializeResolverTypeInterceptor(builder);

        if (builder.ContextData.TryGetValue(WellKnownContextData.ResolverConfigs, out var o) &&
            o is List<FieldResolverConfig> resolverConfigs)
        {
            resolverConfigs.Add(new(fieldCoordinate, resolver, null, resultType));
        }

        return builder;
    }

    private static ISchemaBuilder AddResolverTypeInternal(
        ISchemaBuilder builder,
        string typeName,
        Type resolverType)
    {
        InitializeResolverTypeInterceptor(builder);

        if (builder.ContextData.TryGetValue(WellKnownContextData.ResolverTypes, out var o) &&
            o is List<(string, Type)> resolverTypes)
        {
            resolverTypes.Add((typeName, resolverType));
        }

        return builder;
    }

    private static void InitializeResolverTypeInterceptor(ISchemaBuilder builder)
    {
        if (!builder.ContextData.ContainsKey(WellKnownContextData.ResolverConfigs))
        {
            var resolverConfigs = new List<FieldResolverConfig>();
            var resolverTypes = new List<(string, Type)>();
            var runtimeTypes = new Dictionary<string, Type>();

            builder.ContextData.Add(WellKnownContextData.ResolverConfigs, resolverConfigs);
            builder.ContextData.Add(WellKnownContextData.ResolverTypes, resolverTypes);
            builder.ContextData.Add(WellKnownContextData.RuntimeTypes, runtimeTypes);

            builder.TryAddTypeInterceptor(
                new ResolverTypeInterceptor(resolverConfigs, resolverTypes, runtimeTypes));
        }
    }
}
