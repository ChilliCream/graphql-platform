using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MassTransit.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mocha.Mediator.Benchmarks.Messaging;

/// <summary>
/// Compares notification publish performance across mediator libraries.
/// Each benchmark resolves mediators from a fresh scope to mirror real request handling.
/// Immediate.Handlers is excluded as it does not support notifications.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class NotificationBenchmarks
{
    private ServiceProvider _rootProvider = null!;
    private IServiceProvider? _mediatorSgProvider;
    private Wolverine.IMessageBus? _wolverineBus;
    private IHost? _wolverineHost;
    private IServiceProvider? _switchMediatorProvider;
    private IServiceProvider? _massTransitProvider;
    private BenchmarkNotificationHandler _handler = null!;
    private BenchmarkNotification _notification = null!;
    private MediatorSgNotification _mediatorSgNotification = null!;
    private WolverineNotification _wolverineNotification = null!;
    private SwitchMediatorNotification _switchMediatorNotification = null!;
    private MassTransitNotification _massTransitNotification = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Mocha + MediatR (shared provider)
        var services = new ServiceCollection();
        MediatorServiceCollectionExtensions.AddMediator(services).AddBenchmarks();
        services.AddMediatR(opts =>
            opts.RegisterServicesFromAssembly(typeof(BenchmarkNotificationHandler).Assembly));
        _rootProvider = services.BuildServiceProvider();

        // martinothamar Mediator (separate provider)
        try
        {
            _mediatorSgProvider = MediatorSgFactory.CreateServiceProvider();
        }
        catch (InvalidOperationException)
        {
        }

        // Wolverine
        try
        {
            (_wolverineHost, _wolverineBus) = WolverineFactory.Create();
        }
        catch (InvalidOperationException)
        {
        }

        // SwitchMediator (separate provider)
        try
        {
            _switchMediatorProvider = SwitchMediatorFactory.CreateServiceProvider();
        }
        catch (InvalidOperationException)
        {
        }

        // MassTransit Mediator (separate provider)
        try
        {
            _massTransitProvider = MassTransitFactory.CreateServiceProvider();
        }
        catch (InvalidOperationException)
        {
        }

        _handler = new BenchmarkNotificationHandler();
        _notification = new BenchmarkNotification(Guid.NewGuid());
        _mediatorSgNotification = new MediatorSgNotification(Guid.NewGuid());
        _wolverineNotification = new WolverineNotification(Guid.NewGuid());
        _switchMediatorNotification = new SwitchMediatorNotification(Guid.NewGuid());
        _massTransitNotification = new MassTransitNotification(Guid.NewGuid());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _wolverineHost?.StopAsync().GetAwaiter().GetResult();
        _wolverineHost?.Dispose();
    }

    [Benchmark]
    public Task PublishNotification_MediatR()
    {
        var scope = _rootProvider.CreateScope();
        var mediatr = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        return mediatr.Publish(_notification, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask PublishNotification_Mocha_IMediator()
    {
        var scope = _rootProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mocha.Mediator.IMediator>();
        return mediator.PublishAsync(_notification, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask PublishNotification_Mocha_Concrete()
    {
        var scope = _rootProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mocha.Mediator.Mediator>();
        return mediator.PublishAsync(_notification, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask PublishNotification_MediatorSg()
    {
        if (_mediatorSgProvider is null)
            return default;

        var scope = _mediatorSgProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<global::Mediator.IMediator>();
        return mediator.Publish(_mediatorSgNotification, CancellationToken.None);
    }

    [Benchmark]
    public Task PublishNotification_Wolverine()
    {
        if (_wolverineBus is not null)
        {
            return _wolverineBus.InvokeAsync(_wolverineNotification, CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    [Benchmark]
    public ValueTask PublishNotification_SwitchMediator()
    {
        if (_switchMediatorProvider is null)
            return default;

        var scope = _switchMediatorProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<global::Mediator.Switch.IValuePublisher>();
        return publisher.Publish(_switchMediatorNotification, CancellationToken.None);
    }

    [Benchmark]
    public Task PublishNotification_MassTransit()
    {
        if (_massTransitProvider is not null)
        {
            var scope = _massTransitProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
            return mediator.Publish<MassTransitNotification>(
                _massTransitNotification, CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    [Benchmark(Baseline = true)]
    public ValueTask PublishNotification_Baseline()
    {
        return _handler.HandleAsync(_notification, CancellationToken.None);
    }
}
