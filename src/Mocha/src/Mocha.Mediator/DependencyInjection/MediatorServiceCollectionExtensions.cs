using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Mocha.Mediator;

/// <summary>
/// Provides extension methods for registering the Mocha Mediator on <see cref="IServiceCollection"/>.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default (unnamed) Mocha Mediator infrastructure to the service collection.
    /// </summary>
    public static IMediatorHostBuilder AddMediator(this IServiceCollection services)
        => AddMediator(services, string.Empty);

    /// <summary>
    /// Adds a named Mocha Mediator infrastructure to the service collection.
    /// </summary>
    public static IMediatorHostBuilder AddMediator(this IServiceCollection services, string name)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);

        services.AddOptions();
        services.AddMediatorPoolingCore();

        if (name.Length == 0)
        {
            services.TryAddSingleton(sp => BuildRuntime(sp, name));

            services.TryAddScoped<Mediator>();
            services.TryAddScoped<IMediator>(sp => sp.GetRequiredService<Mediator>());
            services.TryAddScoped<ISender>(sp => sp.GetRequiredService<Mediator>());
            services.TryAddScoped<IPublisher>(sp => sp.GetRequiredService<Mediator>());
        }
        else
        {
            services.TryAddKeyedSingleton(name, (sp, _) => BuildRuntime(sp, name));

            services.TryAddKeyedScoped(name,
                static (sp, key) => new Mediator(
                    sp.GetRequiredKeyedService<MediatorRuntime>(key),
                    sp));
            services.TryAddKeyedScoped<IMediator>(name,
                static (sp, key) => sp.GetRequiredKeyedService<Mediator>(key));
            services.TryAddKeyedScoped<ISender>(name,
                static (sp, key) => sp.GetRequiredKeyedService<Mediator>(key));
            services.TryAddKeyedScoped<IPublisher>(name,
                static (sp, key) => sp.GetRequiredKeyedService<Mediator>(key));
        }

        return new MediatorHostBuilder(services, name);
    }

    private static MediatorRuntime BuildRuntime(IServiceProvider sp, string name)
    {
        var setup = sp.GetRequiredService<IOptionsMonitor<MediatorSetup>>().Get(name);

        var builder = new MediatorBuilder();

        foreach (var configure in setup.ConfigureMediator)
        {
            configure(builder);
        }

        return builder.Build(sp);
    }
}
