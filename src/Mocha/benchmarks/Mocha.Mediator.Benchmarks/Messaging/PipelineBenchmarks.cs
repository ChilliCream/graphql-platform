using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Benchmarks.Messaging;

/// <summary>
/// Compares pipeline behavior overhead across mediator libraries.
/// Each benchmark uses a singleton mediator resolved once during setup.
/// Wolverine and MassTransit are excluded as they use fundamentally different middleware models.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PipelineBenchmarks
{
    private MediatR.IMediator _mediatr = null!;
    private Mocha.Mediator.IMediator _mocha = null!;
    private global::Mediator.IMediator? _mediatorSg;
    private global::Mediator.Switch.IValueSender? _switchMediator;
    private IHandler<ImmediatePipelineCommandHandler.Command, BenchmarkResponse>? _immediateHandler;
    private BenchmarkCommand _command = null!;
    private MediatorSgCommand _mediatorSgCommand = null!;
    private SwitchMediatorCommand _switchMediatorCommand = null!;
    private ImmediatePipelineCommandHandler.Command _immediateCommand = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Mocha + MediatR (shared provider)
        var services = new ServiceCollection();
        var builder = MediatorServiceCollectionExtensions.AddMediator(services);
        builder.Use(BenchmarkMochaMiddleware.Create());
        builder.AddBenchmarks();
        services.AddMediatR(opts =>
        {
            opts.RegisterServicesFromAssembly(typeof(BenchmarkCommandHandler).Assembly);
            opts.AddBehavior<MediatR.IPipelineBehavior<BenchmarkCommand, BenchmarkResponse>,
                BenchmarkMediatRPipelineBehavior>();
        });
        var rootProvider = services.BuildServiceProvider();
        _mediatr = rootProvider.GetRequiredService<MediatR.IMediator>();
        _mocha = rootProvider.GetRequiredService<Mocha.Mediator.IMediator>();

        // martinothamar Mediator with pipeline behavior
        try
        {
            var provider = MediatorSgFactory.CreateServiceProviderWithPipeline();
            _mediatorSg = provider.GetRequiredService<global::Mediator.IMediator>();
        }
        catch (InvalidOperationException)
        {
        }

        // SwitchMediator with pipeline behavior
        try
        {
            var provider = SwitchMediatorFactory.CreateServiceProviderWithPipeline();
            _switchMediator = provider.GetRequiredService<global::Mediator.Switch.IValueSender>();
        }
        catch (InvalidOperationException)
        {
        }

        // Immediate.Handlers with pipeline behavior
        try
        {
            var provider = ImmediateHandlersFactory.CreateServiceProviderWithPipeline();
            _immediateHandler = provider.GetRequiredService<IHandler<ImmediatePipelineCommandHandler.Command, BenchmarkResponse>>();
        }
        catch (InvalidOperationException)
        {
        }

        _command = new BenchmarkCommand(Guid.NewGuid());
        _mediatorSgCommand = new MediatorSgCommand(Guid.NewGuid());
        _switchMediatorCommand = new SwitchMediatorCommand(Guid.NewGuid());
        _immediateCommand = new ImmediatePipelineCommandHandler.Command(Guid.NewGuid());
    }

    [Benchmark(Baseline = true)]
    public Task<BenchmarkResponse> WithPipeline_MediatR()
    {
        return _mediatr.Send(_command, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> WithPipeline_Mocha()
    {
        return _mocha.SendAsync<BenchmarkResponse>(_command, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<BenchmarkResponse> WithPipeline_MediatorSg()
    {
        if (_mediatorSg is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        return _mediatorSg.Send(_mediatorSgCommand, CancellationToken.None);
    }

    // [Benchmark]
    public ValueTask<BenchmarkResponse> WithPipeline_SwitchMediator()
    {
        if (_switchMediator is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        return _switchMediator.Send<BenchmarkResponse>(_switchMediatorCommand, CancellationToken.None);
    }

    // [Benchmark]
    public ValueTask<BenchmarkResponse> WithPipeline_ImmediateHandlers()
    {
        if (_immediateHandler is null)
            return new ValueTask<BenchmarkResponse>(new BenchmarkResponse(Guid.Empty));

        return _immediateHandler.HandleAsync(_immediateCommand, CancellationToken.None);
    }
}
