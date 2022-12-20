``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT


```
main
|                                         Method |       Mean |     Error |   StdDev |     Median | Rank |    Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |-----------:|----------:|---------:|-----------:|-----:|---------:|---------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |   428.2 μs |  12.76 μs | 37.02 μs |   429.3 μs |    1 |  41.9922 |        - |       - |     84 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |   633.6 μs |  16.05 μs | 47.32 μs |   624.2 μs |    2 |  65.4297 |        - |       - |    129 KB |
|                                Sessions_Medium | 2,552.7 μs |  50.09 μs | 73.42 μs | 2,535.3 μs |    3 | 417.9688 | 175.7813 | 11.7188 |  1,096 KB |
|                                 Sessions_Large | 5,784.6 μs | 113.96 μs | 95.16 μs | 5,756.6 μs |    4 | 820.3125 | 390.6250 | 23.4375 |  2,278 KB |

No String
|                                         Method |       Mean |    Error |    StdDev |     Median | Rank |    Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |-----------:|---------:|----------:|-----------:|-----:|---------:|---------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |   477.3 μs |  9.49 μs |  20.01 μs |   477.8 μs |    1 |  41.9922 |        - |       - |     83 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |   706.8 μs | 36.83 μs | 108.60 μs |   741.7 μs |    2 |  62.5000 |        - |       - |    125 KB |
|                                Sessions_Medium | 2,276.8 μs | 21.99 μs |  19.49 μs | 2,280.5 μs |    3 | 351.5625 | 144.5313 |  7.8125 |  1,049 KB |
|                                 Sessions_Large | 5,321.1 μs | 90.12 μs |  84.30 μs | 5,319.9 μs |    4 | 835.9375 | 414.0625 | 23.4375 |  2,222 KB |

12.2.1
|                                         Method |       Mean |    Error |   StdDev |     Median | Rank |    Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |-----------:|---------:|---------:|-----------:|-----:|---------:|---------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |   379.3 μs |  3.82 μs |  3.58 μs |   378.2 μs |    1 |  42.4805 |        - |       - |     85 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |   604.2 μs |  9.93 μs |  8.80 μs |   603.7 μs |    2 |  66.4063 |        - |       - |    130 KB |
|                                Sessions_Medium | 2,484.9 μs | 45.38 μs | 44.57 μs | 2,467.2 μs |    3 | 394.5313 | 156.2500 | 11.7188 |  1,082 KB |
|                                 Sessions_Large | 5,574.5 μs | 99.04 μs | 92.64 μs | 5,572.2 μs |    4 | 812.5000 | 382.8125 | 31.2500 |  2,254 KB |

12.0.1
|                                         Method |       Mean |     Error |    StdDev |     Median | Rank |    Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |-----------:|----------:|----------:|-----------:|-----:|---------:|---------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |   383.3 μs |   7.47 μs |  10.47 μs |   379.9 μs |    1 |  41.9922 |        - |       - |     84 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |   606.6 μs |   9.35 μs |   9.60 μs |   602.6 μs |    2 |  66.4063 |        - |       - |    131 KB |
|                                Sessions_Medium | 2,459.8 μs |  19.75 μs |  17.51 μs | 2,462.2 μs |    3 | 429.6875 | 167.9688 | 11.7188 |  1,095 KB |
|                                 Sessions_Large | 5,887.7 μs | 114.85 μs | 117.94 μs | 5,863.0 μs |    4 | 804.6875 | 390.6250 | 31.2500 |  2,242 KB |
