``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                                         Method |       Mean |     Error |    StdDev |     Median | Rank |    Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |-----------:|----------:|----------:|-----------:|-----:|---------:|--------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |   352.8 μs |   1.24 μs |   1.16 μs |   352.9 μs |    1 |   8.3008 |  1.9531 |       - |     84 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |   625.7 μs |   3.03 μs |   2.69 μs |   625.5 μs |    2 |  12.6953 |  2.9297 |       - |    130 KB |
|                                Sessions_Medium | 2,685.6 μs |  52.66 μs | 130.16 μs | 2,691.1 μs |    3 | 101.5625 | 39.0625 |  7.8125 |  1,081 KB |
|                                 Sessions_Large | 6,816.8 μs | 134.29 μs | 294.77 μs | 6,759.6 μs |    4 | 218.7500 | 62.5000 | 15.6250 |  2,294 KB |
