using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Add a GraphQL root type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="rootType">
    /// A type representing a GraphQL root type.
    /// This type must inherit from <see cref="ObjectType{T}"/> or be a class.
    /// </param>
    /// <param name="operation">
    /// The operation type that <paramref name="rootType"/> represents.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="rootType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <paramref name="rootType"/> is either not a class or is not inheriting from
    /// <see cref="ObjectType{T}"/>.
    ///
    /// - A root type for the specified <paramref name="operation"/> was already set.
    /// </exception>
    public static IRequestExecutorBuilder AddRootType(
        this IRequestExecutorBuilder builder,
        Type rootType,
        OperationType operation)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (rootType is null)
        {
            throw new ArgumentNullException(nameof(rootType));
        }

        return builder.ConfigureSchema(b => b.AddRootType(rootType, operation));
    }

    /// <summary>
    /// Add a GraphQL root type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="rootType">
    /// An instance of <see cref="ObjectType"/> that represents a root type.
    /// </param>
    /// <param name="operation">
    /// The operation type that <paramref name="rootType"/> represents.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="rootType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A root type for the specified <paramref name="operation"/> was already set.
    /// </exception>
    public static IRequestExecutorBuilder AddRootType(
        this IRequestExecutorBuilder builder,
        ObjectType rootType,
        OperationType operation)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (rootType is null)
        {
            throw new ArgumentNullException(nameof(rootType));
        }

        return builder.ConfigureSchema(b => b.AddRootType(rootType, operation));
    }

    /// <summary>
    /// Add a GraphQL query type with the name `Query`.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A query type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryType(
        this IRequestExecutorBuilder builder) =>
        AddQueryType(builder, d => d.Name(OperationTypeNames.Query));

    /// <summary>
    /// Add a GraphQL query type with the name `Query` and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A query type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryType(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddQueryType(d =>
        {
            d.Name(OperationTypeNames.Query);
            configure(d);
        }));
    }

    /// <summary>
    /// Add a GraphQL query type with the name `Query` and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <typeparam name="T">
    /// The query runtime type.
    /// </typeparam>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <typeparamref name="T"/> is either not a class or is a schema type.
    ///
    /// - A query type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryType<T>(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor<T>> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddQueryType(configure));
    }

    /// <summary>
    /// Add a GraphQL query type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="queryType">
    /// A type representing the GraphQL query root type.
    /// This type must inherit from <see cref="ObjectType{T}"/> or be a class.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="queryType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <paramref name="queryType"/> is either not a class or is not inheriting from
    /// <see cref="ObjectType{T}"/>.
    ///
    /// - A query type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryType(
        this IRequestExecutorBuilder builder,
        Type queryType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (queryType is null)
        {
            throw new ArgumentNullException(nameof(queryType));
        }

        return builder.ConfigureSchema(b => b.AddQueryType(queryType));
    }

    /// <summary>
    /// Add a GraphQL query type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="queryType">
    /// An instance of <see cref="ObjectType"/> that represents the query type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="queryType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A query type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryType(
        this IRequestExecutorBuilder builder,
        ObjectType queryType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (queryType is null)
        {
            throw new ArgumentNullException(nameof(queryType));
        }

        return builder.ConfigureSchema(b => b.AddQueryType(queryType));
    }

    /// <summary>
    /// Add a GraphQL query type.
    /// </summary>
    /// <typeparam name="TQuery">
    /// The query type.
    /// </typeparam>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <typeparamref name="TQuery"/> is either not a class or is not inheriting from
    /// <see cref="ObjectType{T}"/>.
    ///
    /// - A query type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryType<TQuery>(
        this IRequestExecutorBuilder builder)
        where TQuery : class
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddQueryType<TQuery>());
    }

    /// <summary>
    /// Add a GraphQL mutation type with the name `Mutation`.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A mutation type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddMutationType(
        this IRequestExecutorBuilder builder) =>
        AddMutationType(builder, d => d.Name(OperationTypeNames.Mutation));

    /// <summary>
    /// Add a GraphQL mutation type with the name `Mutation` and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - A mutation type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddMutationType(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddMutationType(d =>
        {
            d.Name(OperationTypeNames.Mutation);
            configure(d);
        }));
    }

    /// <summary>
    /// Add a GraphQL mutation type with the name `Mutation` and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <typeparam name="T">
    /// The mutation runtime type.
    /// </typeparam>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <typeparamref name="T"/> is either not a class or is a schema type.
    ///
    /// - A mutation type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddMutationType<T>(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor<T>> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddMutationType(configure));
    }

    /// <summary>
    /// Add a GraphQL mutation type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="mutationType">
    /// A type representing the GraphQL query root type.
    /// This type must inherit from <see cref="ObjectType{T}"/> or be a class.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="mutationType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <paramref name="mutationType"/> is either not a class or is not inheriting from
    /// <see cref="ObjectType{T}"/>.
    ///
    /// - A mutation type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddMutationType(
        this IRequestExecutorBuilder builder,
        Type mutationType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (mutationType is null)
        {
            throw new ArgumentNullException(nameof(mutationType));
        }

        return builder.ConfigureSchema(b => b.AddMutationType(mutationType));
    }

    /// <summary>
    /// Add a GraphQL mutation type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="mutationType">
    /// An instance of <see cref="ObjectType"/> that represents the query type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="mutationType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A query type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddMutationType(
        this IRequestExecutorBuilder builder,
        ObjectType mutationType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (mutationType is null)
        {
            throw new ArgumentNullException(nameof(mutationType));
        }

        return builder.ConfigureSchema(b => b.AddMutationType(mutationType));
    }

    /// <summary>
    /// Add a GraphQL mutation type.
    /// </summary>
    /// <typeparam name="TMutation">
    /// The mutation type.
    /// </typeparam>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <typeparamref name="TMutation"/> is either not a class or is not inheriting from
    /// <see cref="ObjectType{T}"/>.
    ///
    /// - A mutation type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddMutationType<TMutation>(
        this IRequestExecutorBuilder builder)
        where TMutation : class
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddMutationType<TMutation>());
    }

    /// <summary>
    /// Add a GraphQL subscription type with the name `Subscription`.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A subscription type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddSubscriptionType(
        this IRequestExecutorBuilder builder) =>
        AddSubscriptionType(builder, d => d.Name(OperationTypeNames.Subscription));

    /// <summary>
    /// Add a GraphQL subscription type with the name `Subscription` and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - A subscription type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddSubscriptionType(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddSubscriptionType(d =>
        {
            d.Name(OperationTypeNames.Subscription);
            configure(d);
        }));
    }

    /// <summary>
    /// Add a GraphQL subscription type with the name `Subscription` and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <typeparam name="T">
    /// The subscription runtime type.
    /// </typeparam>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <typeparamref name="T"/> is either not a class or is a schema type.
    ///
    /// - A subscription type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddSubscriptionType<T>(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor<T>> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddSubscriptionType(configure));
    }

    /// <summary>
    /// Add a GraphQL subscription type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="subscriptionType">
    /// A type representing the GraphQL subscription root type.
    /// This type must inherit from <see cref="ObjectType{T}"/> or be a class.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="subscriptionType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <paramref name="subscriptionType"/> is either not a class or is not inheriting from
    /// <see cref="ObjectType{T}"/>.
    ///
    /// - A subscription type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddSubscriptionType(
        this IRequestExecutorBuilder builder,
        Type subscriptionType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (subscriptionType is null)
        {
            throw new ArgumentNullException(nameof(subscriptionType));
        }

        return builder.ConfigureSchema(b => b.AddSubscriptionType(subscriptionType));
    }

    /// <summary>
    /// Add a GraphQL subscription type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="subscriptionType">
    /// An instance of <see cref="ObjectType"/> that represents the query type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="subscriptionType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A subscription type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddSubscriptionType(
        this IRequestExecutorBuilder builder,
        ObjectType subscriptionType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (subscriptionType is null)
        {
            throw new ArgumentNullException(nameof(subscriptionType));
        }

        return builder.ConfigureSchema(b => b.AddSubscriptionType(subscriptionType));
    }

    /// <summary>
    /// Add a GraphQL subscription type.
    /// </summary>
    /// <typeparam name="TSubscription">
    /// The subscription type.
    /// </typeparam>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <typeparamref name="TSubscription"/> is either not a class or is not inheriting from
    /// <see cref="ObjectType{T}"/>.
    ///
    /// - A subscription type was already added.
    /// </exception>
    public static IRequestExecutorBuilder AddSubscriptionType<TSubscription>(
        this IRequestExecutorBuilder builder)
        where TSubscription : class
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddSubscriptionType<TSubscription>());
    }

    /// <summary>
    /// This helper adds a new GraphQL object type and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddObjectType(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddObjectType(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL object type which will be inferred from the
    /// provided <typeparamref name="T"/>.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddObjectType<T>(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddObjectType<T>());
    }

    /// <summary>
    /// This helper adds a new GraphQL object type which will be inferred from the
    /// provided <typeparamref name="T"/> and applies the <paramref name="configure"/>
    /// delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddObjectType<T>(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor<T>> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddObjectType(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL union type and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddUnionType(
       this IRequestExecutorBuilder builder,
       Action<IUnionTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddUnionType(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL union type which will be inferred from the
    /// provided <typeparamref name="T"/>.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddUnionType<T>(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddUnionType<T>());
    }

    /// <summary>
    /// This helper adds a new GraphQL union type which will be inferred from the
    /// provided <typeparamref name="T"/> and applies the <paramref name="configure"/>
    /// delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddUnionType<T>(
        this IRequestExecutorBuilder builder,
        Action<IUnionTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddUnionType<T>(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL enum type and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddEnumType(
       this IRequestExecutorBuilder builder,
       Action<IEnumTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddEnumType(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL enum type which will be inferred from the
    /// provided <typeparamref name="T"/>.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddEnumType<T>(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddEnumType<T>());
    }

    /// <summary>
    /// This helper adds a new GraphQL enum type which will be inferred from the
    /// provided <typeparamref name="T"/> and applies the <paramref name="configure"/>
    /// delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddEnumType<T>(
        this IRequestExecutorBuilder builder,
        Action<IEnumTypeDescriptor<T>> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddEnumType(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL interface type and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddInterfaceType(
       this IRequestExecutorBuilder builder,
       Action<IInterfaceTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddInterfaceType(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL interface type which will be inferred from the
    /// provided <typeparamref name="T"/>.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddInterfaceType<T>(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddInterfaceType<T>());
    }

    /// <summary>
    /// This helper adds a new GraphQL interface type which will be inferred from the
    /// provided <typeparamref name="T"/> and applies the <paramref name="configure"/>
    /// delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddInterfaceType<T>(
        this IRequestExecutorBuilder builder,
        Action<IInterfaceTypeDescriptor<T>> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddInterfaceType(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL input object type and applies the
    /// <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddInputObjectType(
       this IRequestExecutorBuilder builder,
       Action<IInputObjectTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddInputObjectType(configure));
    }

    /// <summary>
    /// This helper adds a new GraphQL input object type which will be inferred from the
    /// provided <typeparamref name="T"/>.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddInputObjectType<T>(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddInputObjectType<T>());
    }

    /// <summary>
    /// This helper adds a new GraphQL input object type which will be inferred from the
    /// provided <typeparamref name="T"/> and applies the <paramref name="configure"/>
    /// delegate.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="T"/> is a schema type.
    /// </exception>
    public static IRequestExecutorBuilder AddInputObjectType<T>(
        this IRequestExecutorBuilder builder,
        Action<IInputObjectTypeDescriptor<T>> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.AddInputObjectType(configure));
    }

    /// <summary>
    /// Adds a GraphQL type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="type">
    /// The GraphQL type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="builder"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddType(
        this IRequestExecutorBuilder builder,
        Type type)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return builder.ConfigureSchema(b => b.AddType(type));
    }

    /// <summary>
    /// Adds a GraphQL type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="namedType">
    /// The GraphQL type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="builder"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddType(
        this IRequestExecutorBuilder builder,
        INamedType namedType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (namedType is null)
        {
            throw new ArgumentNullException(nameof(namedType));
        }

        return builder.ConfigureSchema(b => b.AddType(namedType));
    }

    /// <summary>
    /// Adds a GraphQL type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="namedTypeFactory">
    /// A factory to create a named type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="builder"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddType(
        this IRequestExecutorBuilder builder,
        Func<INamedType> namedTypeFactory)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (namedTypeFactory is null)
        {
            throw new ArgumentNullException(nameof(namedTypeFactory));
        }

        return builder.ConfigureSchema(sb => sb.AddType(namedTypeFactory()));
    }

    /// <summary>
    /// Adds a GraphQL type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="namedTypeFactory">
    /// A factory to create a named type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="builder"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddType(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, INamedType> namedTypeFactory)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (namedTypeFactory is null)
        {
            throw new ArgumentNullException(nameof(namedTypeFactory));
        }

        return builder.ConfigureSchema((sp, sb) => sb.AddType(namedTypeFactory(sp)));
    }

    /// <summary>
    /// Adds a GraphQL type to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <typeparam name="T">
    /// The GraphQL type.
    /// </typeparam>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="builder"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddType<T>(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddType<T>());
    }

    public static IRequestExecutorBuilder AddTypes(
        this IRequestExecutorBuilder builder,
        params Type[] types)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (types is null)
        {
            throw new ArgumentNullException(nameof(types));
        }

        return builder.ConfigureSchema(b => b.AddTypes(types));
    }

    public static IRequestExecutorBuilder AddTypes(
        this IRequestExecutorBuilder builder,
        params INamedType[] types)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (types is null)
        {
            throw new ArgumentNullException(nameof(types));
        }

        return builder.ConfigureSchema(b => b.AddTypes(types));
    }

    public static IRequestExecutorBuilder AddDirectiveType(
        this IRequestExecutorBuilder builder,
        Type directiveType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (directiveType is null)
        {
            throw new ArgumentNullException(nameof(directiveType));
        }

        return builder.ConfigureSchema(b => b.AddDirectiveType(directiveType));
    }

    public static IRequestExecutorBuilder AddDirectiveType<TDirective>(
        this IRequestExecutorBuilder builder)
        where TDirective : DirectiveType
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddDirectiveType<TDirective>());
    }

    public static IRequestExecutorBuilder AddDirectiveType(
        this IRequestExecutorBuilder builder,
        DirectiveType directiveType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (directiveType is null)
        {
            throw new ArgumentNullException(nameof(directiveType));
        }

        return builder.ConfigureSchema(b => b.AddDirectiveType(directiveType));
    }

    public static IRequestExecutorBuilder SetSchema<TSchema>(
        this IRequestExecutorBuilder builder)
        where TSchema : ISchema
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.SetSchema<TSchema>());
    }

    public static IRequestExecutorBuilder SetSchema(
        this IRequestExecutorBuilder builder,
        Type schemaType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (schemaType is null)
        {
            throw new ArgumentNullException(nameof(schemaType));
        }

        return builder.ConfigureSchema(b => b.SetSchema(schemaType));
    }

    public static IRequestExecutorBuilder SetSchema(
        this IRequestExecutorBuilder builder,
        ISchema schema)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        return builder.ConfigureSchema(b => b.SetSchema(schema));
    }

    public static IRequestExecutorBuilder SetSchema(
        this IRequestExecutorBuilder builder,
        Action<ISchemaTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        return builder.ConfigureSchema(b => b.SetSchema(configure));
    }

    public static IRequestExecutorBuilder AddTypeExtension(
        this IRequestExecutorBuilder builder,
        INamedTypeExtension typeExtension)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeExtension is null)
        {
            throw new ArgumentNullException(nameof(typeExtension));
        }

        return builder.ConfigureSchema(b => b.AddType(typeExtension));
    }

    public static IRequestExecutorBuilder AddTypeExtension(
        this IRequestExecutorBuilder builder,
        Type typeExtension)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeExtension is null)
        {
            throw new ArgumentNullException(nameof(typeExtension));
        }

        return builder.ConfigureSchema(b => b.AddType(typeExtension));
    }

    public static IRequestExecutorBuilder AddTypeExtension<TExtension>(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddType<TExtension>());
    }

    /// <summary>
    /// Adds an object type extension and applies the <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">The GraphQL configuration builder.</param>
    /// <param name="configure">A delegate to configure the type.</param>
    /// <typeparam name="TExtension">The extension type.</typeparam>
    /// <returns>The GraphQL configuration builder.</returns>
    public static IRequestExecutorBuilder AddObjectTypeExtension<TExtension>(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor<TExtension>> configure)
    {
        return builder.ConfigureSchema(
            b => b.AddType(new ObjectTypeExtension<TExtension>(configure)));
    }

    /// <summary>
    /// Adds an object type extension and applies an optional <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">The GraphQL configuration builder.</param>
    /// <param name="configure">A delegate to configure the type.</param>
    /// <typeparam name="TExtension">The extension type.</typeparam>
    /// <typeparam name="TExtends">The type to extend.</typeparam>
    /// <returns>The GraphQL configuration builder.</returns>
    public static IRequestExecutorBuilder AddObjectTypeExtension<TExtension, TExtends>(
        this IRequestExecutorBuilder builder,
        Action<IObjectTypeDescriptor<TExtension>>? configure = null)
    {
        return builder.ConfigureSchema(
            b => b.AddType(new ObjectTypeExtension<TExtension>(d =>
            {
                d.ExtendsType<TExtends>();
                configure?.Invoke(d);
            })));
    }

    /// <summary>
    /// Adds an object type extension and applies an optional <paramref name="configure"/> delegate.
    /// </summary>
    /// <param name="builder">The GraphQL configuration builder.</param>
    /// <param name="objectTypeName">The name of the object type to extend.</param>
    /// <param name="configure">A delegate to configure the type.</param>
    /// <typeparam name="TExtension">The extension type.</typeparam>
    /// <returns>The GraphQL configuration builder.</returns>
    public static IRequestExecutorBuilder AddObjectTypeExtension<TExtension>(
        this IRequestExecutorBuilder builder,
        string objectTypeName,
        Action<IObjectTypeDescriptor<TExtension>>? configure = null)
    {
        return builder.ConfigureSchema(
            b => b.AddType(new ObjectTypeExtension<TExtension>(d =>
            {
                d.Name(objectTypeName);
                configure?.Invoke(d);
            })));
    }

    public static IRequestExecutorBuilder BindRuntimeType<TRuntimeType, TSchemaType>(
        this IRequestExecutorBuilder builder)
        where TSchemaType : INamedType
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.BindRuntimeType<TRuntimeType, TSchemaType>());
    }

    public static IRequestExecutorBuilder BindRuntimeType(
        this IRequestExecutorBuilder builder,
        Type runtimeType,
        Type schemaType)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (runtimeType is null)
        {
            throw new ArgumentNullException(nameof(runtimeType));
        }

        if (schemaType is null)
        {
            throw new ArgumentNullException(nameof(schemaType));
        }

        return builder.ConfigureSchema(b => b.BindRuntimeType(runtimeType, schemaType));
    }

    public static IRequestExecutorBuilder BindRuntimeType<TRuntimeType>(
        this IRequestExecutorBuilder builder,
        string? typeName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        typeName ??= typeof(TRuntimeType).Name;
        typeName.EnsureGraphQLName();

        return builder.ConfigureSchema(b => b.BindRuntimeType<TRuntimeType>(typeName));
    }

    public static IRequestExecutorBuilder BindRuntimeType(
        this IRequestExecutorBuilder builder,
        Type runtimeType,
        string? typeName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (runtimeType is null)
        {
            throw new ArgumentNullException(nameof(runtimeType));
        }

        typeName ??= runtimeType.Name;
        typeName.EnsureGraphQLName();

        return builder.ConfigureSchema(b => b.BindRuntimeType(runtimeType, typeName));
    }
}
