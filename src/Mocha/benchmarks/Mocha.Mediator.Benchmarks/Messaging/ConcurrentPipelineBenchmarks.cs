using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
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
public class ConcurrentPipelineBenchmarks
{
    private MediatR.IMediator _mediatr = null!;
    private Mocha.Mediator.IMediator _mocha = null!;
    private global::Mediator.IMediator? _mediatorSg;
    private BenchmarkCommand _command = null!;
    private MediatorSgCommand _mediatorSgCommand = null!;

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

        _command = new BenchmarkCommand(Guid.NewGuid());
        _mediatorSgCommand = new MediatorSgCommand(Guid.NewGuid());
    }

    [Benchmark(Baseline = true)]
    public async Task WithPipeline_MediatR()
    {
        var tasks = new Task<BenchmarkResponse>[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _mediatr.Send(_command, CancellationToken.None);
        }

        for (var i = 0; i < tasks.Length; i++)
        {
            _ = await tasks[i];
        }
    }

    [Benchmark]
    public async ValueTask WithPipeline_Mocha()
    {
        var tasks = new ValueTask<BenchmarkResponse>[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _mocha.SendAsync<BenchmarkResponse>(_command, CancellationToken.None);
        }

        for (var i = 0; i < tasks.Length; i++)
        {
            _ = await tasks[i];
        }
    }

    [Benchmark]
    public async ValueTask WithPipeline_MediatorSg()
    {
        var tasks = new ValueTask<BenchmarkResponse>[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _mediatorSg!.Send(_mediatorSgCommand, CancellationToken.None);
        }

        for (var i = 0; i < tasks.Length; i++)
        {
            _ = await tasks[i]!;
        }
    }
}
