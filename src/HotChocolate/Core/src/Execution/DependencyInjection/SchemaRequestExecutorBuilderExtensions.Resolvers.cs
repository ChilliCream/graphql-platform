using System.Linq.Expressions;
using System.Reflection;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

// ReSharper disable once CheckNamespace
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
        string typeName,
        string fieldName,
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
        string typeName,
        string fieldName,
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
        string? typeName = null)
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

        return builder.ConfigureSchema(b => b.AddResolver(resolverType, typeName));
    }

    /// <summary>
    /// Adds a custom parameter expression builder to the resolver compiler.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IResolverCompiler"/>.
    /// </param>
    /// <param name="expression">
    /// A expression that resolves the data for the custom parameter.
    /// </param>
    /// <param name="canHandle">
    /// A predicate that can be used to specify to which parameter the
    /// expression shall be applied to.
    /// </param>
    /// <typeparam name="T">
    /// The parameter result type.
    /// </typeparam>
    /// <returns>
    /// An <see cref="IResolverCompiler"/> that can be used to configure to
    /// chain in more configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddParameterExpressionBuilder<T>(
        this IRequestExecutorBuilder builder,
        Expression<Func<IResolverContext, T>> expression,
        Func<ParameterInfo, bool>? canHandle = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        if (canHandle is null)
        {
            builder.Services.AddParameterExpressionBuilder(
                _ => new CustomParameterExpressionBuilder<T>(expression));
        }
        else
        {
            builder.Services.AddParameterExpressionBuilder(
                _ => new CustomParameterExpressionBuilder<T>(expression, canHandle));
        }

        return builder;
    }
}
