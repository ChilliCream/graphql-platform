using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="InputParser"/> to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder to which the <see cref="InputParser"/> is added.
    /// </param>
    /// <param name="configure">
    /// The configuration delegate that configures the <see cref="InputParserOptions"/>.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to which the <see cref="InputParser"/> was added
    /// for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c> or
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddInputParser(
        this IRequestExecutorBuilder builder,
        Action<InputParserOptions> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.Services.AddInputParser(configure);
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="InputParser"/> to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the <see cref="InputParser"/> is added.
    /// </param>
    /// <param name="configure">
    /// The configuration delegate that configures the <see cref="InputParserOptions"/>.
    /// </param>
    /// <returns>
    /// Returns the service collection to which the <see cref="InputParser"/> was added
    /// for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <c>null</c> or
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddInputParser(
        this IServiceCollection services,
        Action<InputParserOptions> configure)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new InputParserOptions();
        configure(options);
        services.AddSingleton(
            sp => new InputParser(
                sp.GetRequiredService<ITypeConverter>(),
                options));
        return services;
    }
}
