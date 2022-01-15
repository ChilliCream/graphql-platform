``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                                         Method |        Mean |     Error |    StdDev |      Median | Rank |    Gen 0 |    Gen 1 | Allocated |
|----------------------------------------------- |------------:|----------:|----------:|------------:|-----:|---------:|---------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |    185.1 μs |   1.23 μs |   1.15 μs |    184.8 μs |    1 |   4.6387 |   0.9766 |     49 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |    511.5 μs |  10.13 μs |  12.81 μs |    511.6 μs |    2 |   8.7891 |   1.9531 |     96 KB |
|                                Sessions_Medium |  2,378.0 μs |  47.14 μs | 112.95 μs |  2,371.0 μs |    3 |  74.2188 |  27.3438 |    764 KB |
|                                 Sessions_Large |  6,213.5 μs | 123.55 μs | 281.39 μs |  6,051.8 μs |    4 | 171.8750 |  15.6250 |  1,818 KB |
|                      Sessions_DataLoader_Large | 15,185.9 μs | 299.54 μs | 332.94 μs | 15,078.8 μs |    5 | 343.7500 | 125.0000 |  3,772 KB |
