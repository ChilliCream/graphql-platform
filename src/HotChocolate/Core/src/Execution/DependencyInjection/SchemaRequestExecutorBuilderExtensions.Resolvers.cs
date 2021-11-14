using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
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
    /// Returns the <see cref="IRequestExecutorBuilder"/> to allow configuration chaining.
    /// </returns>
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
    public static IRequestExecutorBuilder AddResolver(
        this IRequestExecutorBuilder builder,
        NameString typeName,
        NameString fieldName,
        object? constantResult)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, constantResult));
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
    public static IRequestExecutorBuilder AddResolver<TResult>(
        this IRequestExecutorBuilder builder,
        NameString typeName,
        NameString fieldName,
        TResult constantResult)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddResolver(typeName, fieldName, constantResult));
    }

    /// <summary>
    /// Adds a resolver delegate for a specific field.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <typeparam name="TResolver">
    /// The type that holds one or many resolvers
    /// for the specified <paramref name="typeName"/>.
    /// </typeparam>
    /// <param name="typeName">
    /// The type to which the resolver is bound.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/> to allow configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddResolver<TResolver>(
        this IRequestExecutorBuilder builder,
        NameString? typeName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddResolver<TResolver>(typeName));
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
    public static IRequestExecutorBuilder AddResolver(
        this IRequestExecutorBuilder builder,
        Type resolverType,
        NameString? typeName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (resolverType is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.AddResolver(resolverType, typeName));
    }
}
