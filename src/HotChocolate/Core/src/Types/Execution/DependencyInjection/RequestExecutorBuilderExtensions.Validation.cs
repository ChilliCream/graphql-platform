using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;

// ReSharper disable once CheckNamespace
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
        ArgumentNullException.ThrowIfNull(builder);
        return ConfigureValidation(builder, (_, b) => b.AddVisitor<T>(isCacheable: isCacheable));
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return ConfigureValidation(
            builder,
            (_, b) => b.AddVisitor(factory, isCacheable: isCacheable));
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
        ArgumentNullException.ThrowIfNull(builder);
        return ConfigureValidation(builder, (_, b) => b.AddRule<T>());
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return ConfigureValidation(builder, (_, b) => b.AddRule(factory));
    }

    /// <summary>
    /// Adds a validation rule that restricts the depth of a GraphQL request.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="maxAllowedExecutionDepth">
    /// The max allowed GraphQL request depth.
    /// </param>
    /// <param name="skipIntrospectionFields">
    /// Specifies if depth analysis is skipped for introspection queries.
    /// </param>
    /// <param name="allowRequestOverrides">
    /// Defines if request depth overrides are allowed on a per-request basis.
    /// </param>
    /// <param name="isEnabled">
    /// Defines if the validation rule is enabled.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddMaxExecutionDepthRule(
        this IRequestExecutorBuilder builder,
        int maxAllowedExecutionDepth,
        bool skipIntrospectionFields = false,
        bool allowRequestOverrides = false,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.AddMaxExecutionDepthRule(
                maxAllowedExecutionDepth,
                skipIntrospectionFields,
                allowRequestOverrides,
                isEnabled));
        return builder;
    }

    /// <summary>
    /// Toggle whether introspection is disabled or not.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="disable">
    /// If `true` introspection is disabled, except for requests
    /// that carry an introspection-allowed flag.
    /// If `false` introspection is enabled.
    /// </param>
    public static IRequestExecutorBuilder DisableIntrospection(
        this IRequestExecutorBuilder builder,
        bool disable = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return ConfigureValidation(
            builder,
            (_, b) => b.ModifyOptions(o => o.DisableIntrospection = disable));
    }

    /// <summary>
    /// Toggle whether introspection is disabled or not.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="disable">
    /// If `true` introspection is disabled, except for requests
    /// that carry an introspection-allowed flag.
    /// If `false` introspection is enabled.
    /// </param>
    public static IRequestExecutorBuilder DisableIntrospection(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, ValidationOptions, bool> disable)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(disable);

        return ConfigureValidation(
            builder,
            (s, b) => b.ModifyOptions(o => o.DisableIntrospection = disable(s, o)));
    }

    /// <summary>
    /// Sets the maximum allowed document validation errors.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="maxAllowedValidationErrors">
    /// The maximum number of validation errors.
    /// </param>
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
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.ModifyOptions(o => o.MaxAllowedErrors = maxAllowedValidationErrors));

        return builder;
    }

    /// <summary>
    /// Sets the maximum allowed locations per validation error.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="maxAllowedLocationsPerError">
    /// The maximum number of locations per validation error.
    /// </param>
    /// <returns>
    /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
    /// configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder SetMaxAllowedLocationsPerValidationError(
        this IRequestExecutorBuilder builder,
        int maxAllowedLocationsPerError)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.ModifyOptions(o => o.MaxLocationsPerError = maxAllowedLocationsPerError));

        return builder;
    }

    /// <summary>
    /// Sets the max allowed depth for introspection queries.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="maxAllowedOfTypeDepth">
    /// The max allowed ofType depth for introspection queries.
    /// </param>
    /// <param name="maxAllowedListRecursiveDepth">
    /// The max allowed list recursive depth for introspection queries.
    /// </param>
    /// <returns>
    /// Returns an <see cref="IRequestExecutorBuilder"/> that can be used to chain
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder SetIntrospectionAllowedDepth(
        this IRequestExecutorBuilder builder,
        ushort maxAllowedOfTypeDepth,
        ushort maxAllowedListRecursiveDepth)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.ModifyOptions(o =>
            {
                o.MaxAllowedOfTypeDepth = maxAllowedOfTypeDepth;
                o.MaxAllowedListRecursiveDepth = maxAllowedListRecursiveDepth;
            }));

        return builder;
    }

    /// <summary>
    /// Adds a validation rule that restricts the coordinate cycle depth in a GraphQL operation.
    /// </summary>
    public static IRequestExecutorBuilder AddMaxAllowedFieldCycleDepthRule(
        this IRequestExecutorBuilder builder,
        ushort? defaultCycleLimit = 3,
        (SchemaCoordinate Coordinate, ushort MaxAllowed)[]? coordinateCycleLimits = null,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.AddMaxAllowedFieldCycleDepthRule(
                defaultCycleLimit,
                coordinateCycleLimits,
                isEnabled));

        return builder;
    }

    /// <summary>
    /// Removes the validation rule that restricts the coordinate cycle depth in a GraphQL operation.
    /// </summary>
    public static IRequestExecutorBuilder RemoveMaxAllowedFieldCycleDepthRule(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(builder, (_, b) => b.RemoveMaxAllowedFieldCycleDepthRule());
        return builder;
    }

    /// <summary>
    /// Configures the underlying <see cref="DocumentValidatorBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// The delegate to configure the <see cref="DocumentValidatorBuilder"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder ConfigureValidation(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, DocumentValidatorBuilder> configure)
    {
        return Configure(builder, options => options.OnBuildDocumentValidatorHooks.Add(configure));
    }
}
