``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT


```

No Strings
|                                         Method |        Mean |      Error |     StdDev |      Median | Rank |     Gen 0 |    Gen 1 | Allocated |
|----------------------------------------------- |------------:|-----------:|-----------:|------------:|-----:|----------:|---------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |    95.19 μs |   0.541 μs |   0.506 μs |    95.29 μs |    1 |   23.8037 |        - |     49 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |   379.46 μs |   9.579 μs |  27.789 μs |   379.22 μs |    2 |   46.8750 |        - |     93 KB |
|                                Sessions_Medium | 1,737.50 μs |  12.734 μs |  11.288 μs | 1,735.08 μs |    3 |  257.8125 |  93.7500 |    716 KB |
|                                 Sessions_Large | 4,360.58 μs |  81.441 μs |  93.787 μs | 4,371.65 μs |    4 |  625.0000 | 226.5625 |  1,716 KB |
|                      Sessions_DataLoader_Large | 9,573.32 μs | 187.099 μs | 192.137 μs | 9,532.06 μs |    5 | 1375.0000 | 546.8750 |  3,749 KB |

12.2.1
|                                         Method |        Mean |     Error |    StdDev |      Median | Rank |     Gen 0 |    Gen 1 | Allocated |
|----------------------------------------------- |------------:|----------:|----------:|------------:|-----:|----------:|---------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |    107.7 μs |   2.09 μs |   3.49 μs |    107.5 μs |    1 |   24.1699 |        - |     50 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |    371.2 μs |   7.26 μs |  12.52 μs |    368.4 μs |    2 |   47.8516 |        - |     96 KB |
|                                Sessions_Medium |  1,819.4 μs |  25.44 μs |  23.79 μs |  1,809.4 μs |    3 |  261.7188 |  99.6094 |    741 KB |
|                                 Sessions_Large |  4,891.7 μs |  73.56 μs |  68.80 μs |  4,891.4 μs |    4 |  695.3125 | 304.6875 |  1,778 KB |
|                      Sessions_DataLoader_Large | 10,463.0 μs | 204.13 μs | 250.69 μs | 10,423.4 μs |    5 | 1406.2500 | 562.5000 |  3,700 KB |

12.0.1
|                                         Method |        Mean |     Error |    StdDev |      Median | Rank |     Gen 0 |    Gen 1 | Allocated |
|----------------------------------------------- |------------:|----------:|----------:|------------:|-----:|----------:|---------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |    111.8 μs |   2.16 μs |   2.40 μs |    112.7 μs |    1 |   24.2920 |        - |     50 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |    368.5 μs |   7.13 μs |   7.00 μs |    368.3 μs |    2 |   47.8516 |        - |     95 KB |
|                                Sessions_Medium |  1,870.8 μs |  13.20 μs |  11.02 μs |  1,869.5 μs |    3 |  263.6719 |  87.8906 |    767 KB |
|                                 Sessions_Large |  4,800.6 μs |  78.29 μs |  73.23 μs |  4,809.3 μs |    4 |  593.7500 | 242.1875 |  1,759 KB |
|                      Sessions_DataLoader_Large | 10,389.6 μs | 133.49 μs | 118.33 μs | 10,368.8 μs |    5 | 1312.5000 | 468.7500 |  3,730 KB |
