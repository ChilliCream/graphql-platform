using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator;

/// <summary>
/// Provides extension methods for configuring the mediator through the host builder.
/// </summary>
public static class MediatorHostBuilderExtensions
{
    /// <summary>
    /// Adds a middleware to the pipeline. When neither <paramref name="before"/> nor <paramref name="after"/>
    /// is specified the middleware is appended to the end of the pipeline.
    /// </summary>
    public static IMediatorHostBuilder Use(
        this IMediatorHostBuilder builder,
        MediatorMiddlewareConfiguration middleware,
        string? before = null,
        string? after = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        builder.ConfigureMediator(b => b.Use(middleware, before: before, after: after));
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="MediatorOptions"/> for this mediator instance.
    /// </summary>
    public static IMediatorHostBuilder ConfigureOptions(
        this IMediatorHostBuilder builder,
        Action<MediatorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.ConfigureMediator(b => b.ConfigureOptions(configure));
        return builder;
    }

    /// <summary>
    /// Adds the default OpenTelemetry-compatible diagnostic event listener to the mediator pipeline.
    /// </summary>
    public static IMediatorHostBuilder AddInstrumentation(this IMediatorHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureMediator(static b => b.AddInstrumentation());
        return builder;
    }

    /// <summary>
    /// Registers a custom <see cref="MediatorDiagnosticEventListener"/> implementation
    /// into the mediator's internal services.
    /// </summary>
    public static IMediatorHostBuilder AddDiagnosticEventListener<T>(this IMediatorHostBuilder builder)
        where T : class, IMediatorDiagnosticEventListener
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureMediator(static b => b.AddDiagnosticEventListener<T>());
        return builder;
    }

    /// <summary>
    /// Registers a diagnostic event listener instance into the mediator's internal services.
    /// </summary>
    public static IMediatorHostBuilder AddDiagnosticEventListener(
        this IMediatorHostBuilder builder,
        IMediatorDiagnosticEventListener listener)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(listener);

        builder.ConfigureMediator(b => b.AddDiagnosticEventListener(listener));

        return builder;
    }

    /// <summary>
    /// Registers additional services into the mediator's internal service collection.
    /// </summary>
    public static IMediatorHostBuilder ConfigureMediatorServices(
        this IMediatorHostBuilder builder,
        Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureServices);

        builder.ConfigureMediator(b => b.ConfigureServices(configureServices));
        return builder;
    }

    /// <summary>
    /// Registers additional services into the mediator's internal service collection,
    /// with access to the application-level service provider.
    /// </summary>
    public static IMediatorHostBuilder ConfigureMediatorServices(
        this IMediatorHostBuilder builder,
        Action<IServiceProvider, IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureServices);

        builder.ConfigureMediator(b => b.ConfigureServices(configureServices));
        return builder;
    }

    /// <summary>
    /// Applies a configuration action directly to the underlying mediator builder.
    /// </summary>
    public static void ConfigureMediator(
        this IMediatorHostBuilder builder,
        Action<IMediatorBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Configure<MediatorSetup>(options => options.ConfigureMediator.Add(configure));
    }

    private static void Configure<TOptions>(
        this IMediatorHostBuilder builder,
        Action<TOptions> configure)
        where TOptions : class
    {
        builder.Services.Configure(builder.Name, configure);
    }
}
