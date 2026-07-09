using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using HotChocolate.Features;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a delegate that will be used to configure a named <see cref="Schema"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configureSchema">
    /// A delegate that is used to configure an <see cref="Schema"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder ConfigureSchema(
        this IRequestExecutorBuilder builder,
        Action<ISchemaBuilder> configureSchema)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureSchema);

        return Configure(
            builder,
            options => options.OnConfigureSchemaBuilderHooks.Add(
                new OnConfigureSchemaBuilderAction(
                    (ctx, _) => configureSchema(ctx.SchemaBuilder))));
    }

    internal static IRequestExecutorBuilder ConfigureSchemaFeature(
        this IRequestExecutorBuilder builder,
        Action<IFeatureCollection> configureSchema)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureSchema);

        return Configure(
            builder,
            options => options.OnConfigureSchemaBuilderHooks.Add(
                new OnConfigureSchemaBuilderAction(
                    (ctx, _) => configureSchema(ctx.SchemaBuilder.Features))));
    }

    /// <summary>
    /// Adds a delegate that will be used to configure a named <see cref="Schema"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configureSchema">
    /// A delegate that is used to configure an <see cref="Schema"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder ConfigureSchemaAsync(
        this IRequestExecutorBuilder builder,
        Func<ISchemaBuilder, CancellationToken, ValueTask> configureSchema)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureSchema);

        return Configure(
            builder,
            options => options.OnConfigureSchemaBuilderHooks.Add(
                new OnConfigureSchemaBuilderAction(
                    (ctx, _, ct) => configureSchema(ctx.SchemaBuilder, ct))));
    }

    /// <summary>
    /// Adds a delegate that will be used to configure a named <see cref="Schema"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configureSchema">
    /// A delegate that is used to configure an <see cref="Schema"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> provided to <paramref name="configureSchema"/>
    /// will be the same application's root service provider instance.
    /// </remarks>
    public static IRequestExecutorBuilder ConfigureSchema(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, ISchemaBuilder> configureSchema)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureSchema);

        return Configure(
            builder,
            options => options.OnConfigureSchemaBuilderHooks.Add(
                new OnConfigureSchemaBuilderAction(
                    (ctx, sp) => configureSchema(sp, ctx.SchemaBuilder))));
    }

    /// <summary>
    /// Adds a delegate that will be used to configure the descriptor context.
    /// </summary>
    public static IRequestExecutorBuilder ConfigureDescriptorContext(
        this IRequestExecutorBuilder builder,
        Action<IDescriptorContext> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return Configure(
            builder,
            options => options.OnConfigureSchemaBuilderHooks.Add(
                new OnConfigureSchemaBuilderAction(
                    (ctx, _) => configure(ctx.DescriptorContext))));
    }

    /// <summary>
    /// Adds a delegate that will be used to configure a named <see cref="Schema"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configureSchema">
    /// A delegate that is used to configure an <see cref="Schema"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> provided to <paramref name="configureSchema"/>
    /// will be the same application's root service provider instance.
    /// </remarks>
    public static IRequestExecutorBuilder ConfigureSchemaAsync(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, ISchemaBuilder, CancellationToken, ValueTask> configureSchema)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureSchema);

        return Configure(
            builder,
            options => options.OnConfigureSchemaBuilderHooks.Add(
                new OnConfigureSchemaBuilderAction(
                    (ctx, sp, ct) => configureSchema(sp, ctx.SchemaBuilder, ct))));
    }

    /// <summary>
    /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
    /// <param name="configure">
    /// A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder ModifyRequestOptions(
        this IRequestExecutorBuilder builder,
        Action<RequestExecutorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return Configure(
            builder,
            options => options.OnConfigureRequestExecutorOptionsHooks.Add(
                new OnConfigureRequestExecutorOptionsAction(
                    (_, opt) => configure(opt))));
    }

    /// <summary>
    /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure
    /// a schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder ModifyRequestOptionsAsync(
        this IRequestExecutorBuilder builder,
        Func<RequestExecutorOptions, CancellationToken, ValueTask> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return Configure(
            builder,
            options => options.OnConfigureRequestExecutorOptionsHooks.Add(
                new OnConfigureRequestExecutorOptionsAction(
                    (_, opt, ct) => configure(opt, ct))));
    }

    /// <summary>
    /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a
    /// schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder ModifyRequestOptions(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, RequestExecutorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return Configure(
            builder,
            (services, options) => options.OnConfigureRequestExecutorOptionsHooks.Add(
                new OnConfigureRequestExecutorOptionsAction(
                    (_, o) => configure(services, o))));
    }

    /// <summary>
    /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="modify">
    /// A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a
    /// schema and its execution.
    /// </returns>
    public static IRequestExecutorBuilder ModifyRequestOptionsAsync(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, RequestExecutorOptions, CancellationToken, ValueTask> modify)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(modify);

        return Configure(
            builder,
            (services, options) => options.OnConfigureRequestExecutorOptionsHooks.Add(
                new OnConfigureRequestExecutorOptionsAction(
                    (_, o, ct) => modify(services, o, ct))));
    }

    /// <summary>
    /// Adds a delegate that will be used to modify the <see cref="RequestParserOptions"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
    /// <param name="configure">
    /// A delegate that is used to modify the <see cref="RequestParserOptions"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder ModifyParserOptions(
        this IRequestExecutorBuilder builder,
        Action<RequestParserOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddSingleton(configure);

        return builder;
    }

    public static IRequestExecutorBuilder ConfigureSchemaServices(
        this IRequestExecutorBuilder builder,
        Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureServices);

        return Configure(
            builder,
            options => options.OnConfigureSchemaServicesHooks.Add(
                (_, sp) => configureServices(sp)));
    }

    public static IRequestExecutorBuilder ConfigureSchemaServices(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureServices);

        return Configure(
            builder,
            options => options.OnConfigureSchemaServicesHooks.Add(
                (ctx, sp) => configureServices(ctx.ApplicationServices, sp)));
    }

    public static IRequestExecutorBuilder ConfigureOnRequestExecutorCreated(
        this IRequestExecutorBuilder builder,
        Action<IRequestExecutor> action)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(action);

        return builder.Configure(
            o => o.OnRequestExecutorCreatedHooks.Add(
                new OnRequestExecutorCreatedAction(
                    (_, re) => action(re))));
    }

    public static IRequestExecutorBuilder ConfigureOnRequestExecutorCreated(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, IRequestExecutor> action)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(action);

        return builder.Configure(
            (s, o) => o.OnRequestExecutorCreatedHooks.Add(
                new OnRequestExecutorCreatedAction(
                    (_, e) => action(s, e))));
    }

    public static IRequestExecutorBuilder ConfigureOnRequestExecutorCreatedAsync(
        this IRequestExecutorBuilder builder,
        Func<IRequestExecutor, CancellationToken, ValueTask> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(asyncAction);

        return builder.Configure(
            o => o.OnRequestExecutorCreatedHooks.Add(
                new OnRequestExecutorCreatedAction(
                    (_, re, ct) => asyncAction(re, ct))));
    }

    public static IRequestExecutorBuilder ConfigureOnRequestExecutorCreatedAsync(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, IRequestExecutor, CancellationToken, ValueTask> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(asyncAction);

        return builder.Configure(
            (s, o) => o.OnRequestExecutorCreatedHooks.Add(
                new OnRequestExecutorCreatedAction(
                    (_, e, ct) => asyncAction(s, e, ct))));
    }

    internal static IRequestExecutorBuilder Configure(
        this IRequestExecutorBuilder builder,
        Action<RequestExecutorSetup> configure)
    {
        builder.Services.Configure(builder.Name, configure);
        return builder;
    }

    internal static IRequestExecutorBuilder Configure(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, RequestExecutorSetup> configure)
    {
        builder.Services.AddTransient<IConfigureOptions<RequestExecutorSetup>>(
            services => new ConfigureNamedOptions<RequestExecutorSetup>(
                builder.Name,
                options => configure(services, options)));

        return builder;
    }
}
