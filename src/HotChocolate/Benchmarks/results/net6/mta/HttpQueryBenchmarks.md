``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1466 (21H2)
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT


```
|                                         Method |       Mean |    Error |   StdDev |     Median | Rank |    Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |-----------:|---------:|---------:|-----------:|-----:|---------:|---------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |   255.8 μs |  0.89 μs |  0.79 μs |   255.9 μs |    1 |  10.2539 |   1.9531 |       - |     83 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |   458.3 μs |  4.03 μs |  3.77 μs |   458.0 μs |    2 |  15.6250 |   3.9063 |       - |    128 KB |
|                                Sessions_Medium | 1,982.5 μs | 15.87 μs | 14.07 μs | 1,983.4 μs |    3 | 125.0000 |  46.8750 |  7.8125 |  1,076 KB |
|                                 Sessions_Large | 5,265.9 μs | 39.77 μs | 37.20 μs | 5,281.7 μs |    4 | 273.4375 | 125.0000 | 23.4375 |  2,287 KB |
