using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Benchmarks.Messaging;

/// <summary>
/// Compares full pipeline performance across mediator libraries.
/// Each library processes a command through 3 middleware stages (pre + behavior + post)
/// using native features where available, or equivalent pipeline behaviors.
/// Each benchmark resolves mediators from a fresh scope to mirror real request handling.
///
/// Mocha: 3 middleware delegates
/// MediatR: pre-processor + pipeline behavior + post-processor (native)
/// MediatorSg/SwitchMediator/Immediate.Handlers: 3 pipeline behaviors (equivalent depth)
///
/// Wolverine and MassTransit are excluded as they use fundamentally different middleware models.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class FullPipelineBenchmarks
{
    private ServiceProvider _rootProvider = null!;
    private IServiceProvider? _mediatorSgProvider;
    private IServiceProvider? _switchMediatorProvider;
    private IServiceProvider? _immediateProvider;
    private FullPipelineCommand _command = null!;
    private FullPipelineMediatorSgCommand _mediatorSgCommand = null!;
    private FullPipelineSwitchMediatorCommand _switchMediatorCommand = null!;
    private ImmediateFullPipelineCommandHandler.Command _immediateCommand = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Mocha + MediatR (shared provider)
        var services = new ServiceCollection();
        var builder = MediatorServiceCollectionExtensions.AddMediator(services);
        builder.Use(FullPipelineMochaPreMiddleware.Create());
        builder.Use(FullPipelineMochaBehaviorMiddleware.Create());
        builder.Use(FullPipelineMochaPostMiddleware.Create());
        builder.AddBenchmarks();
        services.AddMediatR(opts =>
        {
            opts.RegisterServicesFromAssembly(typeof(FullPipelineCommandHandler).Assembly);
            opts.AddBehavior<MediatR.IPipelineBehavior<FullPipelineCommand, BenchmarkResponse>,
                FullPipelineMediatRBehavior>();
        });
        _rootProvider = services.BuildServiceProvider();

        // MediatorSg with 3 behaviors (separate provider)
        try
        {
            _mediatorSgProvider = MediatorSgFactory.CreateServiceProviderWithFullPipeline();
        }
        catch (InvalidOperationException)
        {
        }

        // SwitchMediator with 3 behaviors (separate provider)
        try
        {
            _switchMediatorProvider = SwitchMediatorFactory.CreateServiceProviderWithFullPipeline();
        }
        catch (InvalidOperationException)
        {
        }

        // Immediate.Handlers with 3 behaviors (separate provider)
        try
        {
            _immediateProvider = ImmediateHandlersFactory.CreateServiceProviderWithPipeline();
        }
        catch (InvalidOperationException)
        {
        }

        _command = new FullPipelineCommand(Guid.NewGuid());
        _mediatorSgCommand = new FullPipelineMediatorSgCommand(Guid.NewGuid());
        _switchMediatorCommand = new FullPipelineSwitchMediatorCommand(Guid.NewGuid());
        _immediateCommand = new ImmediateFullPipelineCommandHandler.Command(Guid.NewGuid());
    }

    [Benchmark]
    public Task<BenchmarkResponse> FullPipeline_MediatR()
    {
        var scope = _rootProvider.CreateScope();
        var mediatr = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        return mediatr.Send(_command, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> FullPipeline_Mocha_IMediator()
    {
        var scope = _rootProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mocha.Mediator.IMediator>();
        return mediator.SendAsync<BenchmarkResponse>(_command, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> FullPipeline_Mocha_Concrete()
    {
        var scope = _rootProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Mocha.Mediator.Mediator>();
        return mediator.SendAsync<BenchmarkResponse>(_command, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> FullPipeline_MediatorSg()
    {
        if (_mediatorSgProvider is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        var scope = _mediatorSgProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<global::Mediator.IMediator>();
        return mediator.Send(_mediatorSgCommand, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> FullPipeline_SwitchMediator()
    {
        if (_switchMediatorProvider is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        var scope = _switchMediatorProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<global::Mediator.Switch.IValueSender>();
        return sender.Send<BenchmarkResponse>(_switchMediatorCommand, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> FullPipeline_ImmediateHandlers()
    {
        if (_immediateProvider is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        var scope = _immediateProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IHandler<ImmediateFullPipelineCommandHandler.Command, BenchmarkResponse>>();
        return handler.HandleAsync(_immediateCommand, CancellationToken.None);
    }

    [Benchmark(Baseline = true)]
    public ValueTask<BenchmarkResponse> FullPipeline_Baseline()
    {
        return new FullPipelineCommandHandler().HandleAsync(_command, CancellationToken.None);
    }
}
