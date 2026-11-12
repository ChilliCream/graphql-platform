using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace Mocha.Mediator.Benchmarks.Messaging;

internal static class MediatorSgFactory
{
    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        MediatorSgHelper.Register(services);
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }

    public static IServiceProvider CreateServiceProviderWithPipeline()
    {
        var services = new ServiceCollection();
        MediatorSgHelper.Register(services);
        services.AddSingleton<
            global::Mediator.IPipelineBehavior<
                MediatorSgCommand,
                BenchmarkResponse>,
            MediatorSgPipelineBehavior>();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }

    public static IServiceProvider CreateServiceProviderWithFullPipeline()
    {
        var services = new ServiceCollection();
        MediatorSgHelper.Register(services);
        // Native pre-processor (MessagePreProcessor<,> implements IPipelineBehavior<,>)
        services.AddSingleton<
            global::Mediator.IPipelineBehavior<
                FullPipelineMediatorSgCommand,
                BenchmarkResponse>,
            FullPipelineMediatorSgPreProcessor>();
        // Pipeline behavior
        services.AddSingleton<
            global::Mediator.IPipelineBehavior<
                FullPipelineMediatorSgCommand,
                BenchmarkResponse>,
            FullPipelineMediatorSgBehavior>();
        // Native post-processor (MessagePostProcessor<,> implements IPipelineBehavior<,>)
        services.AddSingleton<
            global::Mediator.IPipelineBehavior<
                FullPipelineMediatorSgCommand,
                BenchmarkResponse>,
            FullPipelineMediatorSgPostProcessor>();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }
}

internal static class WolverineFactory
{
    public static (IHost Host, IMessageBus Bus) Create()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.UseWolverine(opts =>
        {
            opts.Discovery.DisableConventionalDiscovery();
            opts.Discovery.IncludeType(typeof(WolverineCommandHandler));
            opts.Discovery.IncludeType(typeof(WolverineNotificationHandler));
        });
        var host = builder.Build();
        host.StartAsync().GetAwaiter().GetResult();
        var bus = host.Services.GetRequiredService<IMessageBus>();
        return (host, bus);
    }
}

internal static class SwitchMediatorFactory
{
    private static void AddSwitchMediator(ServiceCollection services)
    {
        global::Mediator.Switch.Extensions.Microsoft.DependencyInjection.ServiceCollectionExtensions
            .AddMediator<BenchmarkSwitchMediator>(services, _ => { });
        // SwitchMediator DI extension may not register concrete handler types;
        // ensure they are available for the generated mediator to resolve.
        services.AddScoped<SwitchMediatorCommandHandler>();
        services.AddScoped<SwitchMediatorNotificationHandler>();
    }

    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        AddSwitchMediator(services);
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }

    public static IServiceProvider CreateServiceProviderWithPipeline()
    {
        var services = new ServiceCollection();
        AddSwitchMediator(services);
        services.AddSingleton<
            global::Mediator.Switch.IValuePipelineBehavior<
                SwitchMediatorCommand,
                BenchmarkResponse>,
            SwitchMediatorPipelineBehavior>();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }

    public static IServiceProvider CreateServiceProviderWithFullPipeline()
    {
        var services = new ServiceCollection();
        AddSwitchMediator(services);
        services.AddScoped<FullPipelineSwitchMediatorCommandHandler>();
        services.AddSingleton<
            global::Mediator.Switch.IValuePipelineBehavior<
                FullPipelineSwitchMediatorCommand,
                BenchmarkResponse>,
            FullPipelineSwitchMediatorPreBehavior>();
        services.AddSingleton<
            global::Mediator.Switch.IValuePipelineBehavior<
                FullPipelineSwitchMediatorCommand,
                BenchmarkResponse>,
            FullPipelineSwitchMediatorMainBehavior>();
        services.AddSingleton<
            global::Mediator.Switch.IValuePipelineBehavior<
                FullPipelineSwitchMediatorCommand,
                BenchmarkResponse>,
            FullPipelineSwitchMediatorPostBehavior>();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }
}

internal static class MassTransitFactory
{
    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMediator(cfg =>
        {
            cfg.AddConsumer<MassTransitCommandConsumer>();
            cfg.AddConsumer<MassTransitNotificationConsumer>();
        });
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }
}

internal static class ImmediateHandlersFactory
{
    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMochaMediatorBenchmarksHandlers();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }

    public static IServiceProvider CreateServiceProviderWithPipeline()
    {
        var services = new ServiceCollection();
        services.AddMochaMediatorBenchmarksBehaviors();
        services.AddMochaMediatorBenchmarksHandlers();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }
}
