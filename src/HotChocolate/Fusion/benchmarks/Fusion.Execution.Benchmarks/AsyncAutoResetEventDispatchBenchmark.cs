using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

#nullable enable

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures continuation dispatch of the executor wakeup primitive
/// <c>AsyncAutoResetEvent</c> (src/HotChocolate/Fusion/src/Fusion.Execution/Execution/
/// AsyncAutoResetEvent.cs). Both dispatch sites (the <c>Set()</c> waiter-wake path at
/// line 70 and the already-signaled <c>OnCompleted</c> path at line 44) use
/// <c>ThreadPool.QueueUserWorkItem(static c =&gt; c(), continuation, preferLocal: true)</c>,
/// which allocates a <c>QueueUserWorkItemCallback&lt;Action&gt;</c> and captures/restores
/// <c>ExecutionContext</c> per wakeup. The event fires once per pending merge (per
/// downstream source-schema response) plus once per node completion
/// (ExecutionState.cs lines 188-201), and the dotTrace eShop profile shows the
/// dispatch lambda (<c>AsyncAutoResetEvent+&lt;&gt;c.&lt;Set&gt;b__9_0</c>) as its own column.
///
/// The candidate stores the continuation in a field, implements
/// <c>IThreadPoolWorkItem</c>, and dispatches via
/// <c>ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true)</c>: no work-item
/// allocation and no ExecutionContext flow. The only continuations are compiler
/// generated async-method-builder boxes that restore their own captured context, and
/// the executor design guarantees a single awaiter with at most one outstanding
/// dispatch (the awaiter only re-registers after its continuation ran).
///
/// <c>StoredSignal</c> drives the already-signaled <c>OnCompleted</c> path
/// deterministically on one thread; <c>PingPong</c> drives the genuine
/// waiter-wake <c>Set()</c> path against a consumer task.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfig))]
public class AsyncAutoResetEventDispatchBenchmark
{
    /// <summary>
    /// BenchmarkDotNet 0.15.8 has no RuntimeMoniker for the net11.0 preview host and
    /// this project pins TargetFramework to net11.0, so out-of-process toolchains can
    /// neither validate nor build a child process here. The job therefore runs in
    /// process with the intended 3 warmup and 10 measurement iterations.
    /// </summary>
    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
            => AddJob(
                Job.Default
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithToolchain(InProcessEmitToolchain.Instance));
    }

    private const int OpsPerInvoke = 10_000;

    private readonly AsyncAutoResetEvent _productA = new();
    private readonly AsyncAutoResetEvent _productB = new();
    private readonly UnsafeDispatchAutoResetEvent _unsafeA = new();
    private readonly UnsafeDispatchAutoResetEvent _unsafeB = new();

    [GlobalSetup]
    public void Setup()
    {
        VerifyBehavior(
            new AsyncAutoResetEventFacade(new AsyncAutoResetEvent()));
        VerifyBehavior(
            new UnsafeDispatchAutoResetEventFacade(new UnsafeDispatchAutoResetEvent()));
    }

    /// <summary>
    /// Asserts the auto-reset contract both variants must share: a stored signal
    /// completes the next await, a waiter is released by Set, each signal is consumed
    /// exactly once, and TryResetToIdle only clears a stored signal.
    /// </summary>
    private static void VerifyBehavior(IAutoResetEventFacade evt)
    {
        RunVerification(evt).GetAwaiter().GetResult();

        static async Task RunVerification(IAutoResetEventFacade evt)
        {
            // stored signal completes the next await
            evt.Set();
            if (!evt.IsSignaled)
            {
                throw new InvalidOperationException("Set with no waiter must store the signal.");
            }

            await evt.WaitAsync().WaitAsync(TimeSpan.FromSeconds(10));

            if (evt.IsSignaled)
            {
                throw new InvalidOperationException("The await must consume the stored signal.");
            }

            // TryResetToIdle clears only a stored signal
            if (evt.TryResetToIdle())
            {
                throw new InvalidOperationException("TryResetToIdle must fail with no stored signal.");
            }

            evt.Set();

            if (!evt.TryResetToIdle() || evt.IsSignaled)
            {
                throw new InvalidOperationException("TryResetToIdle must clear a stored signal.");
            }

            // a genuine waiter is released by Set
            var waiter = evt.WaitAsync();
            await Task.Delay(50);

            if (waiter.IsCompleted)
            {
                throw new InvalidOperationException("The await must suspend when no signal is stored.");
            }

            evt.Set();
            await waiter.WaitAsync(TimeSpan.FromSeconds(10));

            // ping-pong ordering: 100 round trips, each signal consumed exactly once
            var a = evt;
            var counter = 0;
            for (var i = 0; i < 100; i++)
            {
                a.Set();
                await a.WaitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                counter++;
            }

            if (counter != 100 || a.IsSignaled)
            {
                throw new InvalidOperationException("Signal must be consumed exactly once per await.");
            }
        }
    }

    [Benchmark(OperationsPerInvoke = OpsPerInvoke, Baseline = true)]
    public async Task StoredSignal_Product()
    {
        for (var i = 0; i < OpsPerInvoke; i++)
        {
            _productA.Set();
            await _productA;
        }
    }

    [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
    public async Task StoredSignal_UnsafeDispatch()
    {
        for (var i = 0; i < OpsPerInvoke; i++)
        {
            _unsafeA.Set();
            await _unsafeA;
        }
    }

    [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
    public async Task PingPong_Product()
    {
        var consumer = Task.Run(async () =>
        {
            for (var i = 0; i < OpsPerInvoke; i++)
            {
                await _productA;
                _productB.Set();
            }
        });

        for (var i = 0; i < OpsPerInvoke; i++)
        {
            _productA.Set();
            await _productB;
        }

        await consumer;
    }

    [Benchmark(OperationsPerInvoke = OpsPerInvoke)]
    public async Task PingPong_UnsafeDispatch()
    {
        var consumer = Task.Run(async () =>
        {
            for (var i = 0; i < OpsPerInvoke; i++)
            {
                await _unsafeA;
                _unsafeB.Set();
            }
        });

        for (var i = 0; i < OpsPerInvoke; i++)
        {
            _unsafeA.Set();
            await _unsafeB;
        }

        await consumer;
    }

    /// <summary>
    /// Benchmark-local candidate: byte-faithful copy of the product
    /// <c>AsyncAutoResetEvent</c> (AsyncAutoResetEvent.cs lines 6-92) with only the two
    /// dispatch sites changed to store the continuation in a field and queue the event
    /// itself via <c>ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true)</c>.
    /// </summary>
    private sealed class UnsafeDispatchAutoResetEvent : INotifyCompletion, IThreadPoolWorkItem
    {
        private readonly Lock _sync = new();
        private Action? _continuation;
        private Action? _scheduled;
        private bool _isSignaled;

        public bool IsSignaled => Volatile.Read(ref _isSignaled);

        public bool IsCompleted => false;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            bool wasSignaled;

            lock (_sync)
            {
                wasSignaled = _isSignaled;

                if (wasSignaled)
                {
                    // consume the signal
                    _isSignaled = false;
                }
                else
                {
                    Debug.Assert(_continuation is null, "There should only be one awaiter.");
                    _continuation = continuation;
                }
            }

            if (wasSignaled)
            {
                Schedule(continuation);
            }
        }

        public void Set()
        {
            Action? continuation = null;

            lock (_sync)
            {
                if (_continuation is not null)
                {
                    // someone is waiting - release them immediately
                    // we don't set _isSignaled since we're consuming it immediately
                    continuation = _continuation;
                    _continuation = null;
                }
                else
                {
                    // since no one waiting we are storing the signal for the next awaiter
                    _isSignaled = true;
                }
            }

            if (continuation is not null)
            {
                Schedule(continuation);
            }
        }

        public bool TryResetToIdle()
        {
            lock (_sync)
            {
                if (_continuation is null && _isSignaled)
                {
                    _isSignaled = false;
                    return true;
                }
                return false;
            }
        }

        public UnsafeDispatchAutoResetEvent GetAwaiter() => this;

        private void Schedule(Action continuation)
        {
            Debug.Assert(_scheduled is null, "At most one dispatch can be outstanding.");
            _scheduled = continuation;
            ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
        }

        void IThreadPoolWorkItem.Execute()
        {
            var continuation = _scheduled!;
            _scheduled = null;
            continuation();
        }
    }

    private interface IAutoResetEventFacade
    {
        bool IsSignaled { get; }
        void Set();
        bool TryResetToIdle();
        Task WaitAsync();
    }

    private sealed class AsyncAutoResetEventFacade(AsyncAutoResetEvent evt) : IAutoResetEventFacade
    {
        public bool IsSignaled => evt.IsSignaled;
        public void Set() => evt.Set();
        public bool TryResetToIdle() => evt.TryResetToIdle();
        public async Task WaitAsync() => await evt;
    }

    private sealed class UnsafeDispatchAutoResetEventFacade(UnsafeDispatchAutoResetEvent evt)
        : IAutoResetEventFacade
    {
        public bool IsSignaled => evt.IsSignaled;
        public void Set() => evt.Set();
        public bool TryResetToIdle() => evt.TryResetToIdle();
        public async Task WaitAsync() => await evt;
    }
}
