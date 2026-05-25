using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Fusion.Execution.Benchmarks;

[ThreadingDiagnoser]
[MemoryDiagnoser]
[InProcess]
[WarmupCount(3)]
[IterationCount(8)]
[InvocationCount(1)]
public class LockStoreBenchmark
{
    private const int DataLength = 256;

    private readonly object _monitorGate = new();
    private readonly Lock _runtimeLock = new();
    private readonly ReaderWriterLockSlim _readerWriterLock = new(LockRecursionPolicy.NoRecursion);
    private SpinLock _spinLock = new(enableThreadOwnerTracking: false);

    private int[] _operationKinds = null!;
    private int[] _operationIndexes = null!;
    private int[] _data = null!;
    private ParallelOptions _parallelOptions = null!;
    private int _sink;

    [Params(8, 32)]
    public int Threads { get; set; }

    [Params(10, 50)]
    public int WritePercent { get; set; }

    [Params(0, 64)]
    public int CriticalSectionWork { get; set; }

    [Params(200_000)]
    public int Operations { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Threads };
        _operationKinds = new int[Operations];
        _operationIndexes = new int[Operations];
        _data = new int[DataLength];

        var seed = unchecked((uint)(17 + Threads * 397 + WritePercent * 17 + CriticalSectionWork));

        for (var i = 0; i < Operations; i++)
        {
            seed = Next(seed);
            _operationKinds[i] = (int)(seed % 100) < WritePercent ? 1 : 0;

            seed = Next(seed);
            _operationIndexes[i] = (int)(seed % DataLength);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        Array.Clear(_data);
        _sink = 0;
    }

    [GlobalCleanup]
    public void Cleanup() => _readerWriterLock.Dispose();

    [Benchmark(Baseline = true)]
    public int MonitorLock() => Run(
        read: i =>
        {
            lock (_monitorGate)
            {
                return ReadCore(i);
            }
        },
        write: i =>
        {
            lock (_monitorGate)
            {
                return WriteCore(i);
            }
        });

    [Benchmark]
    public int RuntimeLock() => Run(
        read: i =>
        {
            lock (_runtimeLock)
            {
                return ReadCore(i);
            }
        },
        write: i =>
        {
            lock (_runtimeLock)
            {
                return WriteCore(i);
            }
        });

    [Benchmark]
    public int ReaderWriterLockSlim() => Run(
        read: i =>
        {
            _readerWriterLock.EnterReadLock();

            try
            {
                return ReadCore(i);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        },
        write: i =>
        {
            _readerWriterLock.EnterWriteLock();

            try
            {
                return WriteCore(i);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        });

    [Benchmark]
    public int SpinLock() => Run(
        read: i =>
        {
            var lockTaken = false;

            try
            {
                _spinLock.Enter(ref lockTaken);
                return ReadCore(i);
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit(useMemoryBarrier: true);
                }
            }
        },
        write: i =>
        {
            var lockTaken = false;

            try
            {
                _spinLock.Enter(ref lockTaken);
                return WriteCore(i);
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit(useMemoryBarrier: true);
                }
            }
        });

    private int Run(Func<int, int> read, Func<int, int> write)
    {
        var total = 0;

        Parallel.For(
            0,
            Operations,
            _parallelOptions,
            static () => 0,
            (i, _, local) =>
            {
                if (_operationKinds[i] == 1)
                {
                    local = unchecked(local + write(i));
                }
                else
                {
                    local = unchecked(local + read(i));
                }

                return local;
            },
            local => Interlocked.Add(ref total, local));

        _sink = total;
        return _sink;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadCore(int operationIndex)
    {
        var dataIndex = _operationIndexes[operationIndex];
        var value = _data[dataIndex];
        return BusyWork(unchecked(value + dataIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WriteCore(int operationIndex)
    {
        var dataIndex = _operationIndexes[operationIndex];
        var value = BusyWork(unchecked(_data[dataIndex] + 1 + dataIndex));
        _data[dataIndex] = value;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int BusyWork(int value)
    {
        var work = CriticalSectionWork;

        for (var i = 0; i < work; i++)
        {
            value = unchecked((value * 1664525) + 1013904223);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Next(uint x)
    {
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        return x;
    }
}
