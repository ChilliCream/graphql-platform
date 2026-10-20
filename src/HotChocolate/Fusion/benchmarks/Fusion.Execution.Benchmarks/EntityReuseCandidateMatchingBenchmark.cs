using System.Numerics;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Compares a linear scan over the lookup variables of the reuse candidates with a probe of
/// a hash table that already indexes them, for targets that either all miss or all hit.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(MatchingConfig))]
public class EntityReuseCandidateMatchingBenchmark
{
    private const int BucketSize = 4;

    private ChunkedArrayWriter _writer = null!;
    private JsonSegment[] _candidates = null!;
    private JsonSegment[] _targets = null!;
    private Entry[] _table = null!;
    private int _bucketCount;

    [Params(1, 2, 3, 4, 6, 8, 16, 64, 256)]
    public int Candidates { get; set; }

    [Params(64)]
    public int Targets { get; set; }

    [Params(false, true)]
    public bool Hit { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _writer = new ChunkedArrayWriter();
        _candidates = new JsonSegment[Candidates];
        _targets = new JsonSegment[Targets];
        _bucketCount = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(Candidates * 4, 16));
        _table = new Entry[_bucketCount * BucketSize];

        for (var i = 0; i < Candidates; i++)
        {
            _candidates[i] = Write(i);
            Add(_candidates[i], i);
        }

        for (var i = 0; i < Targets; i++)
        {
            _targets[i] = Write(Hit ? i % Candidates : Candidates + i);
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _writer.Dispose();

    [Benchmark(Baseline = true)]
    public int Linear()
    {
        var hits = 0;

        foreach (var target in _targets)
        {
            foreach (var candidate in _candidates)
            {
                if (candidate.Equals(target))
                {
                    hits++;
                    break;
                }
            }
        }

        return hits;
    }

    [Benchmark]
    public int Probe()
    {
        var hits = 0;

        foreach (var target in _targets)
        {
            if (TryGet(target, out _))
            {
                hits++;
            }
        }

        return hits;
    }

    private JsonSegment Write(int id)
    {
        var bytes = Encoding.UTF8.GetBytes($"{{\"__fusion_1_id\":\"user-{id}\"}}");
        var start = _writer.Position;
        bytes.CopyTo(_writer.GetSpan(bytes.Length));
        _writer.Advance(bytes.Length);
        return JsonSegment.Create(_writer, start, bytes.Length);
    }

    private void Add(JsonSegment segment, int index)
    {
        var hash = _writer.GetHashCode(segment.Location, segment.Length);
        var start = (hash & 0x7FFFFFFF & (_bucketCount - 1)) * BucketSize;

        for (var s = start; s < start + BucketSize; s++)
        {
            if (_table[s].Index == 0)
            {
                _table[s] = new Entry(hash, index + 1, segment.Location, segment.Length);
                return;
            }
        }

        throw new InvalidOperationException("The bucket is full.");
    }

    private bool TryGet(JsonSegment segment, out int index)
    {
        var hash = _writer.GetHashCode(segment.Location, segment.Length);
        var start = (hash & 0x7FFFFFFF & (_bucketCount - 1)) * BucketSize;

        for (var s = start; s < start + BucketSize; s++)
        {
            ref var entry = ref _table[s];

            if (entry.Index == 0)
            {
                break;
            }

            if (entry.Hash == hash
                && entry.Length == segment.Length
                && _writer.SequenceEqual(entry.Location, segment.Location, segment.Length))
            {
                index = entry.Index - 1;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private readonly record struct Entry(int Hash, int Index, int Location, int Length);

    private sealed class MatchingConfig : ManualConfig
    {
        public MatchingConfig()
        {
            AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(3)
                .WithIterationCount(8)
                .WithLaunchCount(1));
        }
    }
}
