using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Immediate.Handlers.Shared;
using MassTransit.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mocha.Mediator.Benchmarks.Messaging;

/// <summary>
/// Compares command dispatch performance across mediator libraries.
/// Each benchmark resolves mediators from a fresh scope to mirror real request handling.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CommandBenchmarks
{
    private ServiceProvider _rootProvider = null!;
    private IServiceProvider? _mediatorSgProvider;
    private Wolverine.IMessageBus? _wolverineBus;
    private IHost? _wolverineHost;
    private IServiceProvider? _switchMediatorProvider;
    private IServiceProvider? _immediateProvider;
    private IServiceProvider? _massTransitProvider;
    private BenchmarkCommandHandler _handler = null!;
    private BenchmarkCommand _command = null!;
    private MediatorSgCommand _mediatorSgCommand = null!;
    private WolverineCommand _wolverineCommand = null!;
    private SwitchMediatorCommand _switchMediatorCommand = null!;
    private ImmediateCommandHandler.Command _immediateCommand = null!;
    private MassTransitCommand _massTransitCommand = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Mocha + MediatR (shared provider)
        var services = new ServiceCollection();
        MediatorServiceCollectionExtensions.AddMediator(services).AddBenchmarks();
        services.AddMediatR(opts =>
            opts.RegisterServicesFromAssembly(typeof(BenchmarkCommandHandler).Assembly));
        _rootProvider = services.BuildServiceProvider();

        // martinothamar Mediator (separate provider to avoid AddMediator collision)
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

        // Immediate.Handlers (separate provider)
        try
        {
            _immediateProvider = ImmediateHandlersFactory.CreateServiceProvider();
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

        _handler = new BenchmarkCommandHandler();
        _command = new BenchmarkCommand(Guid.NewGuid());
        _mediatorSgCommand = new MediatorSgCommand(Guid.NewGuid());
        _wolverineCommand = new WolverineCommand(Guid.NewGuid());
        _switchMediatorCommand = new SwitchMediatorCommand(Guid.NewGuid());
        _immediateCommand = new ImmediateCommandHandler.Command(Guid.NewGuid());
        _massTransitCommand = new MassTransitCommand(Guid.NewGuid());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _wolverineHost?.StopAsync().GetAwaiter().GetResult();
        _wolverineHost?.Dispose();
    }

    [Benchmark]
    public Task<BenchmarkResponse> SendCommand_MediatR()
    {
        var scope = _rootProvider.CreateScope();
        var mediatr = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        return mediatr.Send(_command, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> SendCommand_Mocha_IMediator()
    {
        var scope = _rootProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mocha.Mediator.IMediator>();
        return mediator.SendAsync<BenchmarkResponse>(_command, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> SendCommand_Mocha_Concrete()
    {
        var scope = _rootProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mocha.Mediator.Mediator>();
        return mediator.SendAsync<BenchmarkResponse>(_command, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> SendCommand_MediatorSg()
    {
        if (_mediatorSgProvider is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        var scope = _mediatorSgProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<global::Mediator.IMediator>();
        return mediator.Send(_mediatorSgCommand, CancellationToken.None);
    }

    [Benchmark]
    public Task<BenchmarkResponse> SendCommand_Wolverine()
    {
        return _wolverineBus?.InvokeAsync<BenchmarkResponse>(_wolverineCommand, CancellationToken.None)
            ?? Task.FromResult(new BenchmarkResponse(Guid.Empty));
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> SendCommand_SwitchMediator()
    {
        if (_switchMediatorProvider is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        var scope = _switchMediatorProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<global::Mediator.Switch.IValueSender>();
        return sender.Send<BenchmarkResponse>(_switchMediatorCommand, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> SendCommand_ImmediateHandlers()
    {
        if (_immediateProvider is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        var scope = _immediateProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IHandler<ImmediateCommandHandler.Command, BenchmarkResponse>>();
        return handler.HandleAsync(_immediateCommand, CancellationToken.None);
    }

    [Benchmark]
    public async Task<MassTransitCommandResponse> SendCommand_MassTransit()
    {
        if (_massTransitProvider is not null)
        {
            var scope = _massTransitProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
            var client = mediator.CreateRequestClient<MassTransitCommand>();
            var response = await client.GetResponse<MassTransitCommandResponse>(
                _massTransitCommand, CancellationToken.None);
            return response.Message;
        }

        return new MassTransitCommandResponse(Guid.Empty);
    }

    [Benchmark(Baseline = true)]
    public ValueTask<BenchmarkResponse> SendCommand_Baseline()
    {
        return _handler.HandleAsync(_command, CancellationToken.None);
    }
}
