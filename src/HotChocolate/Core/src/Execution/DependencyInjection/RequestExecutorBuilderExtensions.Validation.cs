using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a query validation visitor to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="isCacheable">
    /// Defines if the result of this rule can be cached and reused on consecutive
    /// validations of the same GraphQL request document.
    /// </param>
    /// <typeparam name="T">
    /// The type of the validator.
    /// </typeparam>
    /// <returns>
    /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
    /// configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddValidationVisitor<T>(
        this IRequestExecutorBuilder builder,
        bool isCacheable = true)
        where T : DocumentValidatorVisitor, new()
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return ConfigureValidation(builder, b => b.TryAddValidationVisitor<T>(isCacheable));
    }

    /// <summary>
    /// Adds a query validation visitor to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The factory that creates the validator instance.
    /// </param>
    /// <param name="isCacheable">
    /// Defines if the result of this rule can be cached and reused on consecutive
    /// validations of the same GraphQL request document.
    /// </param>
    /// <typeparam name="T">
    /// The type of the validator.
    /// </typeparam>
    /// <returns>
    /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
    /// configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddValidationVisitor<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory,
        bool isCacheable = true)
        where T : DocumentValidatorVisitor
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return ConfigureValidation(
            builder,
            b => b.TryAddValidationVisitor(factory, isCacheable));
    }

    /// <summary>
    /// Adds a query validation visitor to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type of the validator.
    /// </typeparam>
    /// <returns>
    /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
    /// configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddValidationRule<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IDocumentValidatorRule, new()
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return ConfigureValidation(builder, b => b.TryAddValidationRule<T>());
    }

    /// <summary>
    /// Adds a query validation rule to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The factory that creates the validator instance.
    /// </param>
    /// <typeparam name="T">
    /// The type of the validator.
    /// </typeparam>
    /// <returns>
    /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
    /// configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddValidationRule<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory)
        where T : class, IDocumentValidatorRule
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return ConfigureValidation(builder, b => b.TryAddValidationRule(factory));
    }

    /// <summary>
    /// Adds a validation rule that inspects if a GraphQL query document
    /// exceeds the maximum allowed operation depth.
    /// </summary>
    public static IRequestExecutorBuilder AddMaxExecutionDepthRule(
        this IRequestExecutorBuilder builder,
        int maxAllowedExecutionDepth)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        ConfigureValidation(builder, b => b.AddMaxExecutionDepthRule(maxAllowedExecutionDepth));
        return builder;
    }

    /// <summary>
    /// Adds a validation rule that inspects if a GraphQL query document
    /// exceeds the maximum allowed operation depth.
    /// </summary>
    public static IRequestExecutorBuilder AddMaxExecutionDepthRule(
        this IRequestExecutorBuilder builder,
        int maxAllowedExecutionDepth,
        bool skipIntrospectionFields)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        ConfigureValidation(
            builder,
            b => b.AddMaxExecutionDepthRule(maxAllowedExecutionDepth, skipIntrospectionFields));
        return builder;
    }

    /// <summary>
    /// Adds a validation rule that only allows requests to use `__schema` or `__type`
    /// if the request carries an introspection allowed flag.
    /// </summary>
    public static IRequestExecutorBuilder AddIntrospectionAllowedRule(
        this IRequestExecutorBuilder builder) =>
        ConfigureValidation(builder, b => b.AddIntrospectionAllowedRule());

    /// <summary>
    /// Toggle whether introspection is allow or not.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="allow">
    /// If `true` introspection is allowed.
    /// If `false` introspection is disallowed, except for requests
    /// that carry an introspection allowed flag.
    /// </param>
    public static IRequestExecutorBuilder AllowIntrospection(
        this IRequestExecutorBuilder builder,
        bool allow)
    {
        if (!allow)
        {
            builder.AddIntrospectionAllowedRule();
        }

        return builder;
    }

    /// <summary>
    /// Sets the max allowed document validation errors.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="maxAllowedValidationErrors"></param>
    /// <returns>
    /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
    /// configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder SetMaxAllowedValidationErrors(
        this IRequestExecutorBuilder builder,
        int maxAllowedValidationErrors)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        ConfigureValidation(
            builder,
            b => b.ConfigureValidation(
                c => c.Modifiers.Add(o => o.MaxAllowedErrors = maxAllowedValidationErrors)));
        return builder;
    }

    private static IRequestExecutorBuilder ConfigureValidation(
        IRequestExecutorBuilder builder,
        Action<IValidationBuilder> configure)
    {
        configure(builder.Services.AddValidation(builder.Name));
        return builder;
    }
}
