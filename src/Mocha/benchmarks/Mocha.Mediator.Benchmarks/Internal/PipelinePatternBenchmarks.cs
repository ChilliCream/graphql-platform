using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Mocha.Mediator;

namespace Mocha.Mediator.Benchmarks.Internal;

/// <summary>
/// Compares different pipeline composition strategies to understand
/// the overhead of building delegate chains per-call vs pre-compiled.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PipelinePatternBenchmarks
{
    public sealed record PipelineRequest(Guid Id);

    private static readonly Guid _responseId = Guid.NewGuid();

    private PipelineRequest _request = null!;

    private MediatorDelegate _directHandler = null!;
    private MediatorMiddleware[] _middlewares = null!;
    private MediatorDelegate _preCompiledPipeline = null!;
    private MediatorContext _context = null!;

    [Params(0, 1, 3)]
    public int BehaviorCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _request = new PipelineRequest(Guid.NewGuid());
        _directHandler = static ctx =>
        {
            ctx.Result = _responseId;
            return ValueTask.CompletedTask;
        };

        _middlewares = new MediatorMiddleware[BehaviorCount];
        for (var i = 0; i < BehaviorCount; i++)
        {
            _middlewares[i] = static (_, next) => ctx => next(ctx);
        }

        // Pre-compile the pipeline once
        _preCompiledPipeline = BuildDelegateChain(_directHandler, _middlewares);

        // Raw inlined (no delegates, no interfaces)
        _inlinedBehavior1 = new InlinedBehavior1();
        _inlinedBehavior3 = new InlinedBehavior3();

        _context = new MediatorContext();
    }

    private void SetupContext()
    {
        _context.Message = _request;
        _context.MessageType = typeof(PipelineRequest);
        _context.ResponseType = typeof(Guid);
        _context.CancellationToken = CancellationToken.None;
    }

    [Benchmark(Baseline = true)]
    public ValueTask DirectHandler()
    {
        SetupContext();
        return _directHandler(_context);
    }

    [Benchmark]
    public ValueTask DelegateChain_PerCall()
    {
        SetupContext();
        var pipeline = BuildDelegateChain(_directHandler, _middlewares);
        return pipeline(_context);
    }

    [Benchmark]
    public ValueTask DelegateChain_PreCompiled()
    {
        SetupContext();
        return _preCompiledPipeline(_context);
    }

    private static MediatorDelegate BuildDelegateChain(
        MediatorDelegate handler,
        MediatorMiddleware[] middlewares)
    {
        var pipeline = handler;
        for (var i = middlewares.Length - 1; i >= 0; i--)
        {
            var next = pipeline;
            var mw = middlewares[i];
            pipeline = mw(new MediatorMiddlewareFactoryContext { Services = null!, Features = null! }, next);
        }
        return pipeline;
    }

    /// <summary>
    /// Simulates what a source generator could emit: direct inline calls
    /// with no delegate indirection and no interface dispatch.
    /// This is the theoretical upper bound for pipeline performance.
    /// </summary>
    [Benchmark]
    public ValueTask<Guid> RawInlined_NoBehaviors()
    {
        return new ValueTask<Guid>(_responseId);
    }

    [Benchmark]
    public ValueTask<Guid> RawInlined_1Behavior()
    {
        return _inlinedBehavior1.HandleDirect(_request, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<Guid> RawInlined_3Behaviors()
    {
        return _inlinedBehavior3.HandleDirect(_request, CancellationToken.None);
    }

    public sealed class InlinedBehavior1
    {
        private readonly InlinedHandler _handler = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<Guid> HandleDirect(PipelineRequest message, CancellationToken ct)
        {
            return _handler.HandleDirect(message, ct);
        }
    }

    public sealed class InlinedBehavior3
    {
        private readonly InlinedBehavior2 _next = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<Guid> HandleDirect(PipelineRequest message, CancellationToken ct)
        {
            return _next.HandleDirect(message, ct);
        }
    }

    public sealed class InlinedBehavior2
    {
        private readonly InlinedBehavior1 _next = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<Guid> HandleDirect(PipelineRequest message, CancellationToken ct)
        {
            return _next.HandleDirect(message, ct);
        }
    }

    public sealed class InlinedHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<Guid> HandleDirect(PipelineRequest message, CancellationToken ct)
        {
            return new ValueTask<Guid>(_responseId);
        }
    }

    private InlinedBehavior1 _inlinedBehavior1 = null!;
    private InlinedBehavior3 _inlinedBehavior3 = null!;
}
