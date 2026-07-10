using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Benchmarks.Internal;

/// <summary>
/// Compares DI resolution patterns vs cached delegate/dictionary lookups
/// to understand the cost of service resolution at dispatch time.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class LookupBenchmarks
{
    private sealed record LookupRequest(Guid Id);

    private sealed class LookupHandler
    {
        private static readonly Guid _result = Guid.NewGuid();
        public Guid Handle(LookupRequest request) => _result;
    }

    private IServiceProvider _serviceProvider = null!;
    private Func<Guid> _cachedDelegate = null!;
    private FrozenDictionary<Type, object> _frozenLookup = null!;
    private Type _handlerType = null!;
    private LookupRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        _request = new LookupRequest(Guid.NewGuid());
        _handlerType = typeof(LookupHandler);

        var services = new ServiceCollection();
        services.AddScoped<LookupHandler>();
        _serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;

        var handler = _serviceProvider.GetRequiredService<LookupHandler>();
        _cachedDelegate = () => handler.Handle(_request);

        _frozenLookup = new Dictionary<Type, object>
        {
            [typeof(LookupHandler)] = handler
        }.ToFrozenDictionary();
    }

    [Benchmark]
    public Guid DI_GetRequiredService()
    {
        var handler = (LookupHandler)_serviceProvider.GetRequiredService(_handlerType);
        return handler.Handle(_request);
    }

    [Benchmark]
    public Guid DI_Scoped_GetRequiredService()
    {
        using var scope = ((IServiceProvider)_serviceProvider).CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<LookupHandler>();
        return handler.Handle(_request);
    }

    [Benchmark(Baseline = true)]
    public Guid CachedDelegate()
    {
        return _cachedDelegate();
    }

    [Benchmark]
    public Guid FrozenDictionary_TypeLookup()
    {
        var handler = (LookupHandler)_frozenLookup[_handlerType];
        return handler.Handle(_request);
    }
}
