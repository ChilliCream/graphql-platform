``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1466 (21H2)
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT


```
|                                         Method |        Mean |    Error |   StdDev |      Median | Rank |    Gen 0 |    Gen 1 | Allocated |
|----------------------------------------------- |------------:|---------:|---------:|------------:|-----:|---------:|---------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |    140.1 μs |  0.39 μs |  0.35 μs |    140.1 μs |    1 |   5.8594 |   1.4648 |     50 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |    349.4 μs |  1.30 μs |  1.22 μs |    349.4 μs |    2 |  11.2305 |   2.4414 |     93 KB |
|                                Sessions_Medium |  1,633.8 μs |  7.34 μs |  6.87 μs |  1,634.9 μs |    3 |  91.7969 |  35.1563 |    747 KB |
|                                 Sessions_Large |  4,424.0 μs | 15.91 μs | 14.88 μs |  4,428.2 μs |    4 | 218.7500 |  70.3125 |  1,790 KB |
|                      Sessions_DataLoader_Large | 11,618.3 μs | 50.63 μs | 47.36 μs | 11,632.4 μs |    5 | 437.5000 | 187.5000 |  3,670 KB |
