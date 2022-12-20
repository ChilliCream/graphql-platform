using System;
using System.Linq;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;
using Microsoft.Extensions.Options;

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

    internal static IValidationBuilder ModifyValidationOptions(
        this IValidationBuilder builder,
        Action<ValidationOptions> configure)
        => builder.ConfigureValidation(m => m.Modifiers.Add(configure));

    public static IValidationBuilder TryAddValidationVisitor<T>(
        this IValidationBuilder builder,
        bool isCacheable = true)
        where T : DocumentValidatorVisitor, new()
    {
        return builder.ConfigureValidation(m =>
            m.Modifiers.Add(o =>
            {
                if (o.Rules.All(t => t.GetType() != typeof(DocumentValidatorRule<T>)))
                {
                    o.Rules.Add(new DocumentValidatorRule<T>(new T(), isCacheable));
                }
            }));
    }

    public static IValidationBuilder TryAddValidationVisitor<T>(
        this IValidationBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory,
        bool isCacheable = true)
        where T : DocumentValidatorVisitor
    {
        return builder.ConfigureValidation((s, m) =>
            m.Modifiers.Add(o =>
            {
                if (o.Rules.All(t => t.GetType() != typeof(DocumentValidatorRule<T>)))
                {
                    o.Rules.Add(new DocumentValidatorRule<T>(factory(s, o), isCacheable));
                }
            }));
    }

    public static IValidationBuilder TryAddValidationRule<T>(
        this IValidationBuilder builder)
        where T : class, IDocumentValidatorRule, new()
    {
        return builder.ConfigureValidation(m =>
            m.Modifiers.Add(o =>
            {
                if (o.Rules.All(t => t.GetType() != typeof(T)))
                {
                    o.Rules.Add(new T());
                }
            }));
    }

    public static IValidationBuilder TryAddValidationRule<T>(
        this IValidationBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory)
        where T : class, IDocumentValidatorRule
    {
        return builder.ConfigureValidation((s, m) =>
            m.Modifiers.Add(o =>
            {
                var instance = factory(s, o);
                if (o.Rules.All(t => t.GetType() != instance.GetType()))
                {
                    o.Rules.Add(instance);
                }
            }));
    }
}
