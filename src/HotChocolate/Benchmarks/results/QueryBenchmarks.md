``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=6.0.100
  [Host]     : .NET Core 5.0.12 (CoreCLR 5.0.1221.52207, CoreFX 5.0.1221.52207), X64 RyuJIT
  DefaultJob : .NET Core 5.0.12 (CoreCLR 5.0.1221.52207, CoreFX 5.0.1221.52207), X64 RyuJIT


```
|                                         Method |        Mean |       Error |       StdDev |      Median | Rank |    Gen 0 |    Gen 1 | Gen 2 |  Allocated |
|----------------------------------------------- |------------:|------------:|-------------:|------------:|-----:|---------:|---------:|------:|-----------:|
|             Sessions_TitleAndAbstract_10_Items |    890.0 μs |    13.76 μs |     12.20 μs |    886.6 μs |    1 |   5.8594 |   0.9766 |     - |   60.21 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |  1,943.3 μs |    33.19 μs |     31.04 μs |  1,930.9 μs |    2 |  11.7188 |        - |     - |  119.41 KB |
|                                Sessions_Medium |  4,662.0 μs |    93.19 μs |    142.30 μs |  4,641.1 μs |    3 |  70.3125 |  15.6250 |     - |  776.59 KB |
|                                 Sessions_Large | 31,311.9 μs | 4,137.37 μs | 11,601.64 μs | 27,508.0 μs |    4 | 125.0000 |        - |     - | 1810.69 KB |
|                      Sessions_DataLoader_Large | 90,044.8 μs | 7,863.17 μs | 21,657.45 μs | 86,870.0 μs |    5 | 333.3333 | 166.6667 |     - |  3759.6 KB |
