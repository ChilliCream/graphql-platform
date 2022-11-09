using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class PoolBenchmark
{
    private readonly ExecutionTaskPool<ResolverTask, ResolverTaskPoolPolicy> _currentPool =
        new(new ResolverTaskPoolPolicy());

    private readonly DefaultObjectPool<ResolverTask> _objectPool = new(new Policy());

    [Benchmark]
    public void Current_Pool()
    {
         var rented = _currentPool.Get();
                    _currentPool.Return(rented);
    }

    [Benchmark]
    public void Standard_Pool()
    {
        var rented = _objectPool.Get();
                    _objectPool.Return(rented);
    }


    private class Policy : PooledObjectPolicy<ResolverTask>
    {
        private readonly DefaultObjectPool<ResolverTask> _objectPool = new(new Policy2());

        public override ResolverTask Create() => new(_objectPool);

        public override bool Return(ResolverTask obj)
            => obj.Reset();

        private class Policy2 : PooledObjectPolicy<ResolverTask>
        {

            public override ResolverTask Create() => throw new NotImplementedException();

            public override bool Return(ResolverTask obj) => throw new NotImplementedException();
        }
    }
}
