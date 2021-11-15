``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=6.0.100
  [Host]     : .NET Core 5.0.12 (CoreCLR 5.0.1221.52207, CoreFX 5.0.1221.52207), X64 RyuJIT
  DefaultJob : .NET Core 5.0.12 (CoreCLR 5.0.1221.52207, CoreFX 5.0.1221.52207), X64 RyuJIT


```
|                                         Method |       Mean |     Error |    StdDev |     Median | Rank |    Gen 0 |   Gen 1 |   Gen 2 |  Allocated |
|----------------------------------------------- |-----------:|----------:|----------:|-----------:|-----:|---------:|--------:|--------:|-----------:|
|             Sessions_TitleAndAbstract_10_Items |   362.0 μs |   3.10 μs |   2.59 μs |   361.7 μs |    1 |   7.8125 |  0.9766 |       - |   84.05 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |   617.2 μs |   1.69 μs |   1.41 μs |   617.3 μs |    2 |  12.6953 |  0.9766 |       - |  129.99 KB |
|                                Sessions_Medium | 2,717.1 μs |  53.94 μs | 128.19 μs | 2,677.7 μs |    3 | 109.3750 | 19.5313 | 11.7188 | 1102.47 KB |
|                                 Sessions_Large | 6,897.9 μs | 128.85 μs | 311.18 μs | 6,815.5 μs |    4 | 218.7500 | 46.8750 | 15.6250 | 2325.03 KB |
