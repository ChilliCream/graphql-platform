using MassTransit;
using MassTransit.Mediator;
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
                global::Mocha.Mediator.Benchmarks.Messaging.MediatorSgCommand,
                global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>,
            global::Mocha.Mediator.Benchmarks.Messaging.MediatorSgPipelineBehavior>();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }

    public static IServiceProvider CreateServiceProviderWithFullPipeline()
    {
        var services = new ServiceCollection();
        MediatorSgHelper.Register(services);
        // Native pre-processor (MessagePreProcessor<,> implements IPipelineBehavior<,>)
        services.AddSingleton<
            global::Mediator.IPipelineBehavior<
                global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineMediatorSgCommand,
                global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>,
            global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineMediatorSgPreProcessor>();
        // Pipeline behavior
        services.AddSingleton<
            global::Mediator.IPipelineBehavior<
                global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineMediatorSgCommand,
                global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>,
            global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineMediatorSgBehavior>();
        // Native post-processor (MessagePostProcessor<,> implements IPipelineBehavior<,>)
        services.AddSingleton<
            global::Mediator.IPipelineBehavior<
                global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineMediatorSgCommand,
                global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>,
            global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineMediatorSgPostProcessor>();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }
}

internal static class WolverineFactory
{
    public static (IHost Host, Wolverine.IMessageBus Bus) Create()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.UseWolverine(opts =>
        {
            opts.Discovery.DisableConventionalDiscovery();
            opts.Discovery.IncludeType(typeof(global::Mocha.Mediator.Benchmarks.Messaging.WolverineCommandHandler));
            opts.Discovery.IncludeType(typeof(global::Mocha.Mediator.Benchmarks.Messaging.WolverineNotificationHandler));
        });
        var host = builder.Build();
        host.StartAsync().GetAwaiter().GetResult();
        var bus = host.Services.GetRequiredService<Wolverine.IMessageBus>();
        return (host, bus);
    }
}

internal static class SwitchMediatorFactory
{
    private static void AddSwitchMediator(ServiceCollection services)
    {
        global::Mediator.Switch.Extensions.Microsoft.DependencyInjection.ServiceCollectionExtensions
            .AddMediator<global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkSwitchMediator>(services, _ => { });
        // SwitchMediator DI extension may not register concrete handler types;
        // ensure they are available for the generated mediator to resolve.
        services.AddScoped<global::Mocha.Mediator.Benchmarks.Messaging.SwitchMediatorCommandHandler>();
        services.AddScoped<global::Mocha.Mediator.Benchmarks.Messaging.SwitchMediatorNotificationHandler>();
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
                global::Mocha.Mediator.Benchmarks.Messaging.SwitchMediatorCommand,
                global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>,
            global::Mocha.Mediator.Benchmarks.Messaging.SwitchMediatorPipelineBehavior>();
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }

    public static IServiceProvider CreateServiceProviderWithFullPipeline()
    {
        var services = new ServiceCollection();
        AddSwitchMediator(services);
        services.AddScoped<global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineSwitchMediatorCommandHandler>();
        services.AddSingleton<
            global::Mediator.Switch.IValuePipelineBehavior<
                global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineSwitchMediatorCommand,
                global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>,
            global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineSwitchMediatorPreBehavior>();
        services.AddSingleton<
            global::Mediator.Switch.IValuePipelineBehavior<
                global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineSwitchMediatorCommand,
                global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>,
            global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineSwitchMediatorMainBehavior>();
        services.AddSingleton<
            global::Mediator.Switch.IValuePipelineBehavior<
                global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineSwitchMediatorCommand,
                global::Mocha.Mediator.Benchmarks.Messaging.BenchmarkResponse>,
            global::Mocha.Mediator.Benchmarks.Messaging.FullPipelineSwitchMediatorPostBehavior>();
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
            cfg.AddConsumer<global::Mocha.Mediator.Benchmarks.Messaging.MassTransitCommandConsumer>();
            cfg.AddConsumer<global::Mocha.Mediator.Benchmarks.Messaging.MassTransitNotificationConsumer>();
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
