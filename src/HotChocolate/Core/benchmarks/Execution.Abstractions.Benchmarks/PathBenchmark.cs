using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace HotChocolate.Execution.Abstractions.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class PathBenchmark
{
    private Path _depth5 = null!;
    private Path _depth10 = null!;
    private Path _depth20 = null!;
    private Path _depth50 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _depth5 = BuildPath(5);
        _depth10 = BuildPath(10);
        _depth20 = BuildPath(20);
        _depth50 = BuildPath(50);
    }

    private static Path BuildPath(int depth)
    {
        var path = Path.Root;
        for (var i = 0; i < depth; i++)
        {
            path = path.Append("field" + i);
            if (i % 3 == 2)
            {
                path = path.Append(i);
            }
        }
        return path;
    }

    // --- Print benchmarks ---

    [Benchmark]
    public string Print_Depth5() => _depth5.Print();

    [Benchmark]
    public string Print_Depth10() => _depth10.Print();

    [Benchmark]
    public string Print_Depth20() => _depth20.Print();

    [Benchmark]
    public string Print_Depth50() => _depth50.Print();

    // --- ToList benchmarks ---

    [Benchmark]
    public IReadOnlyList<object> ToList_Depth5() => _depth5.ToList();

    [Benchmark]
    public IReadOnlyList<object> ToList_Depth10() => _depth10.ToList();

    [Benchmark]
    public IReadOnlyList<object> ToList_Depth20() => _depth20.ToList();

    [Benchmark]
    public IReadOnlyList<object> ToList_Depth50() => _depth50.ToList();

    // --- EnumerateSegments benchmarks ---

    [Benchmark]
    public int EnumerateSegments_Depth10()
    {
        var count = 0;
        foreach (var _ in _depth10.EnumerateSegments())
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int EnumerateSegments_Depth50()
    {
        var count = 0;
        foreach (var _ in _depth50.EnumerateSegments())
        {
            count++;
        }
        return count;
    }
}
