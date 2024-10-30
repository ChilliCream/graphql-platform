using HotChocolate.Validation;
using HotChocolate.Validation.Options;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IValidationBuilder"/>
/// </summary>
public static partial class HotChocolateValidationBuilderExtensions
{
    /// <summary>
    /// Configures the GraphQL request validation that will be used to validate
    /// incoming GraphQL requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IValidationBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate that is used to configure the <see cref="ValidationOptions"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IValidationBuilder"/> that can be used to configure
    /// the GraphQL request validation.
    /// </returns>
    public static IValidationBuilder ConfigureValidation(
        this IValidationBuilder builder,
        Action<ValidationOptionsModifiers> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.Services.Configure(builder.Name, configure);

        return builder;
    }

    /// <summary>
    /// Configures the GraphQL request validation that will be used to validate
    /// incoming GraphQL requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure the <see cref="ValidationOptions"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IValidationBuilder"/> that can be used to configure
    /// the GraphQL request validation.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> provided to <paramref name="configureClient"/>
    /// will be the application's root service provider instance.
    /// </remarks>
    public static IValidationBuilder ConfigureValidation(
        this IValidationBuilder builder,
        Action<IServiceProvider, ValidationOptionsModifiers> configureClient)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configureClient is null)
        {
            throw new ArgumentNullException(nameof(configureClient));
        }

        builder.Services.AddTransient<IConfigureOptions<ValidationOptionsModifiers>>(sp =>
            new ConfigureNamedOptions<ValidationOptionsModifiers>(
                builder.Name,
                modifier => configureClient(sp, modifier)));

        return builder;
    }

    /// <summary>
    /// Modifies the validation options object.
    /// </summary>
    /// <param name="builder">
    /// The validation builder.
    /// </param>
    /// <param name="configure">
    /// The delegate to mutate the validation options.
    /// </param>
    /// <returns>
    /// Returns the validation builder for configuration chaining.
    /// </returns>
    public static IValidationBuilder ModifyValidationOptions(
        this IValidationBuilder builder,
        Action<ValidationOptions> configure)
        => builder.ConfigureValidation(m => m.Modifiers.Add(configure));

    /// <summary>
    /// Modifies the validation options object.
    /// </summary>
    /// <param name="builder">
    /// The validation builder.
    /// </param>
    /// <param name="configure">
    /// The delegate to mutate the validation options.
    /// </param>
    /// <returns>
    /// Returns the validation builder for configuration chaining.
    /// </returns>
    public static IValidationBuilder ModifyValidationOptions(
        this IValidationBuilder builder,
        Action<IServiceProvider, ValidationOptions> configure)
        => builder.ConfigureValidation((s, m) => m.Modifiers.Add(o => configure(s, o)));

    /// <summary>
    /// Registers the specified validation visitor,
    /// if the same type of validation visitor was not yet registered.
    /// </summary>
    /// <param name="builder">
    /// The validation builder.
    /// </param>
    /// <param name="isCacheable">
    /// Specifies if the validation visitor`s results are cacheable or
    /// if the visitor needs to be rerun on every request.
    /// </param>
    /// <param name="priority">
    /// The priority of the validation visitor. The lower the value the earlier the visitor is executed.
    /// </param>
    /// <typeparam name="T">The validation visitor type.</typeparam>
    /// <returns>
    /// Returns the validation builder for configuration chaining.
    /// </returns>
    public static IValidationBuilder TryAddValidationVisitor<T>(
        this IValidationBuilder builder,
        bool isCacheable = true,
        ushort priority = ushort.MaxValue)
        where T : DocumentValidatorVisitor, new()
    {
        return builder.ConfigureValidation(m =>
            m.RulesModifiers.Add((_, r) =>
            {
                if (r.Rules.All(t => t.GetType() != typeof(DocumentValidatorRule<T>)))
                {
                    r.Rules.Add(new DocumentValidatorRule<T>(new T(), isCacheable, priority));
                }
            }));
    }

    /// <summary>
    /// Registers the specified validation visitor,
    /// if the same type of validation visitor was not yet registered.
    /// </summary>
    /// <param name="builder">
    /// The validation builder.
    /// </param>
    /// <param name="factory">
    /// A factory to create the validation visitor.
    /// </param>
    /// <param name="isCacheable">
    /// Specifies if the validation visitor`s results are cacheable or
    /// if the visitor needs to be rerun on every request.
    /// </param>
    /// <param name="priority">
    /// The priority of the validation visitor. The lower the value the earlier the visitor is executed.
    /// </param>
    /// <param name="isEnabled">
    /// A delegate to determine if the validation visitor and should be added.
    /// </param>
    /// <typeparam name="T">The validation visitor type.</typeparam>
    /// <returns>
    /// Returns the validation builder for configuration chaining.
    /// </returns>
    public static IValidationBuilder TryAddValidationVisitor<T>(
        this IValidationBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory,
        bool isCacheable = true,
        ushort priority = ushort.MaxValue,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
        where T : DocumentValidatorVisitor
    {
        return builder.ConfigureValidation((s, m) =>
            m.RulesModifiers.Add((o, r) =>
            {
                if (r.Rules.All(t => t.GetType() != typeof(DocumentValidatorRule<T>))
                    && (isEnabled?.Invoke(s, o) ?? true))
                {
                    r.Rules.Add(new DocumentValidatorRule<T>(factory(s, o), isCacheable, priority));
                }
            }));
    }

    /// <summary>
    /// Removes the specified validation visitor from the configuration.
    /// </summary>
    public static IValidationBuilder TryRemoveValidationVisitor<T>(
        this IValidationBuilder builder)
        where T : DocumentValidatorVisitor
    {
        return builder.ConfigureValidation((_, m) =>
            m.RulesModifiers.Add((_, r) =>
            {
                var entries = r.Rules.Where(t => t.GetType() == typeof(DocumentValidatorRule<T>)).ToList();
                foreach (var entry in entries)
                {
                    r.Rules.Remove(entry);
                }
            }));
    }

    /// <summary>
    /// Registers the specified validation rule,
    /// if the same type of validation rule was not yet registered.
    /// </summary>
    /// <param name="builder">
    /// The validation builder.
    /// </param>
    /// <typeparam name="T">The validation rule type.</typeparam>
    /// <returns>
    /// Returns the validation builder for configuration chaining.
    /// </returns>
    public static IValidationBuilder TryAddValidationRule<T>(
        this IValidationBuilder builder)
        where T : class, IDocumentValidatorRule, new()
    {
        return builder.ConfigureValidation(m =>
            m.RulesModifiers.Add((_, r) =>
            {
                if (r.Rules.All(t => t.GetType() != typeof(T)))
                {
                    r.Rules.Add(new T());
                }
            }));
    }

    /// <summary>
    /// Registers the specified validation rule,
    /// if the same type of validation rule was not yet registered.
    /// </summary>
    /// <param name="builder">
    /// The validation builder.
    /// </param>
    /// <param name="factory">
    /// A factory to create the validation rule.
    /// </param>
    /// <typeparam name="T">The validation rule type.</typeparam>
    /// <returns>
    /// Returns the validation builder for configuration chaining.
    /// </returns>
    public static IValidationBuilder TryAddValidationRule<T>(
        this IValidationBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory)
        where T : class, IDocumentValidatorRule
    {
        return builder.ConfigureValidation((s, m) =>
            m.RulesModifiers.Add((o, r) =>
            {
                var instance = factory(s, o);
                if (r.Rules.All(t => t.GetType() != instance.GetType()))
                {
                    r.Rules.Add(instance);
                }
            }));
    }

    /// <summary>
    /// Registers the specified validation result aggregator,
    /// if the same type of validation result aggregator was not yet registered.
    /// </summary>
    /// <param name="builder">
    /// The validation builder.
    /// </param>
    /// <param name="factory">
    /// A factory to create the validation result aggregator.
    /// </param>
    /// <typeparam name="T">The validation result aggregator type.</typeparam>
    /// <returns>
    /// Returns the validation builder for configuration chaining.
    /// </returns>
    public static IValidationBuilder TryAddValidationResultAggregator<T>(
        this IValidationBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory)
        where T : class, IValidationResultAggregator
    {
        return builder.ConfigureValidation((s, m) =>
            m.RulesModifiers.Add((o, r) =>
            {
                var instance = factory(s, o);
                if (r.ResultAggregators.All(t => t.GetType() != instance.GetType()))
                {
                    r.ResultAggregators.Add(instance);
                }
            }));
    }
}
