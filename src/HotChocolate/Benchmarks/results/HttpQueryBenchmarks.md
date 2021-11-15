``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 5.0.12 (5.0.1221.52207), X64 RyuJIT
  DefaultJob : .NET 5.0.12 (5.0.1221.52207), X64 RyuJIT


```
main
|                                         Method |      Mean |     Error |    StdDev |    Median | Rank |    Gen 0 |   Gen 1 |  Gen 2 | Allocated |
|----------------------------------------------- |----------:|----------:|----------:|----------:|-----:|---------:|--------:|-------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |  1.063 ms | 0.0200 ms | 0.0187 ms |  1.055 ms |    1 |   9.7656 |       - |      - |    118 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |  2.056 ms | 0.0404 ms | 0.0415 ms |  2.050 ms |    2 |  15.6250 |       - |      - |    179 KB |
|                                Sessions_Medium |  4.878 ms | 0.0972 ms | 0.1363 ms |  4.841 ms |    3 | 101.5625 | 39.0625 | 7.8125 |  1,093 KB |
|                                 Sessions_Large | 17.222 ms | 0.3136 ms | 0.2618 ms | 17.264 ms |    4 | 187.5000 | 31.2500 |      - |  2,321 KB |

12.2.1
|                                         Method |      Mean |     Error |    StdDev |    Median | Rank |    Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |----------:|----------:|----------:|----------:|-----:|---------:|--------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |  1.124 ms | 0.0083 ms | 0.0077 ms |  1.125 ms |    1 |  11.7188 |  1.9531 |       - |    121 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |  2.115 ms | 0.0181 ms | 0.0169 ms |  2.117 ms |    2 |  15.6250 |  3.9063 |       - |    191 KB |
|                                Sessions_Medium |  5.137 ms | 0.1024 ms | 0.1138 ms |  5.105 ms |    3 | 117.1875 | 31.2500 | 15.6250 |  1,148 KB |
|                                 Sessions_Large | 18.754 ms | 0.2336 ms | 0.2185 ms | 18.793 ms |    4 | 218.7500 | 31.2500 | 31.2500 |  2,366 KB |

12.0.1
|                                         Method |      Mean |     Error |    StdDev |    Median | Rank |    Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |----------:|----------:|----------:|----------:|-----:|---------:|--------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |  1.123 ms | 0.0094 ms | 0.0088 ms |  1.121 ms |    1 |  11.7188 |  1.9531 |       - |    122 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |  2.090 ms | 0.0240 ms | 0.0213 ms |  2.088 ms |    2 |  15.6250 |  3.9063 |       - |    192 KB |
|                                Sessions_Medium |  5.105 ms | 0.1000 ms | 0.1027 ms |  5.092 ms |    3 | 117.1875 | 23.4375 | 15.6250 |  1,149 KB |
|                                 Sessions_Large | 18.940 ms | 0.3203 ms | 0.2839 ms | 18.911 ms |    4 | 218.7500 | 31.2500 | 31.2500 |  2,399 KB |