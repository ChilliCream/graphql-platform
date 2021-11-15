``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 5.0.12 (5.0.1221.52207), X64 RyuJIT
  DefaultJob : .NET 5.0.12 (5.0.1221.52207), X64 RyuJIT


```
main
|                                         Method |        Mean |       Error |       StdDev |      Median | Rank |    Gen 0 |   Gen 1 | Allocated |
|----------------------------------------------- |------------:|------------:|-------------:|------------:|-----:|---------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |    938.3 μs |     7.36 μs |      6.15 μs |    936.8 μs |    1 |   5.8594 |       - |     60 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |  2,038.8 μs |    40.28 μs |     44.77 μs |  2,016.4 μs |    2 |  11.7188 |       - |    121 KB |
|                                Sessions_Medium |  4,986.5 μs |    89.75 μs |    149.95 μs |  5,002.0 μs |    3 |  70.3125 | 23.4375 |    761 KB |
|                                 Sessions_Large | 41,416.4 μs | 5,316.53 μs | 15,508.58 μs | 38,161.0 μs |    4 | 156.2500 |       - |  1,827 KB |
|                      Sessions_DataLoader_Large | 71,109.0 μs | 1,399.38 μs |  2,762.24 μs | 70,062.8 μs |    5 |        - |       - |  3,810 KB |

12.2.1
|                                         Method |        Mean |     Error |    StdDev |      Median | Rank |    Gen 0 |    Gen 1 | Allocated |
|----------------------------------------------- |------------:|----------:|----------:|------------:|-----:|---------:|---------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |    884.1 μs |   5.78 μs |   5.12 μs |    885.0 μs |    1 |   5.8594 |   0.9766 |     60 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |  1,924.6 μs |  27.31 μs |  24.21 μs |  1,923.2 μs |    2 |  11.7188 |        - |    120 KB |
|                                Sessions_Medium |  4,738.6 μs |  93.15 μs | 127.51 μs |  4,757.8 μs |    3 |  70.3125 |  23.4375 |    758 KB |
|                                 Sessions_Large | 18,418.6 μs | 326.90 μs | 305.78 μs | 18,329.8 μs |    4 | 156.2500 |        - |  1,796 KB |
|                      Sessions_DataLoader_Large | 67,779.5 μs | 629.83 μs | 589.14 μs | 67,741.2 μs |    5 | 250.0000 | 125.0000 |  3,732 KB |

12.0.1
|                                         Method |        Mean |     Error |    StdDev |      Median | Rank |    Gen 0 |    Gen 1 | Allocated |
|----------------------------------------------- |------------:|----------:|----------:|------------:|-----:|---------:|---------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |    875.4 μs |   8.00 μs |   7.49 μs |    874.2 μs |    1 |   5.8594 |        - |     61 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |  1,906.3 μs |  22.51 μs |  21.05 μs |  1,906.1 μs |    2 |  11.7188 |        - |    120 KB |
|                                Sessions_Medium |  4,680.6 μs |  91.10 μs | 136.36 μs |  4,663.5 μs |    3 |  70.3125 |  23.4375 |    761 KB |
|                                 Sessions_Large | 18,036.4 μs | 223.10 μs | 197.77 μs | 18,072.0 μs |    4 | 156.2500 |  31.2500 |  1,783 KB |
|                      Sessions_DataLoader_Large | 67,417.8 μs | 662.39 μs | 619.60 μs | 67,278.1 μs |    5 | 250.0000 | 125.0000 |  3,697 KB |
