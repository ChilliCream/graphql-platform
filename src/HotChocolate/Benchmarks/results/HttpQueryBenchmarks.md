``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 5.0.12 (5.0.1221.52207), X64 RyuJIT
  DefaultJob : .NET 5.0.12 (5.0.1221.52207), X64 RyuJIT


```
|                                         Method |      Mean |     Error |     StdDev |    Median | Rank |    Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|----------------------------------------------- |----------:|----------:|-----------:|----------:|-----:|---------:|--------:|--------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |  1.120 ms | 0.0096 ms |  0.0085 ms |  1.119 ms |    1 |  11.7188 |  1.9531 |       - |    120 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items |  2.081 ms | 0.0218 ms |  0.0193 ms |  2.084 ms |    2 |  15.6250 |  3.9063 |       - |    190 KB |
|                                Sessions_Medium |  4.999 ms | 0.0934 ms |  0.0874 ms |  4.982 ms |    3 | 109.3750 | 23.4375 |  7.8125 |  1,171 KB |
|                                 Sessions_Large | 37.946 ms | 4.3162 ms | 12.5220 ms | 33.716 ms |    4 | 218.7500 | 62.5000 | 31.2500 |  2,386 KB |
