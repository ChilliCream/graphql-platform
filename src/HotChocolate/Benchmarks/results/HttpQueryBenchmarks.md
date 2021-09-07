``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 12.0 (21A5506j) [Darwin 21.1.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.400
  [Host]     : .NET Core 5.0.9 (CoreCLR 5.0.921.35908, CoreFX 5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET Core 5.0.9 (CoreCLR 5.0.921.35908, CoreFX 5.0.921.35908), X64 RyuJIT


```
|                                         Method |     Mean |     Error |    StdDev |   Median | Rank |    Gen 0 |   Gen 1 |  Gen 2 |  Allocated |
|----------------------------------------------- |---------:|----------:|----------:|---------:|-----:|---------:|--------:|-------:|-----------:|
|             Sessions_TitleAndAbstract_10_Items | 1.049 ms | 0.0059 ms | 0.0056 ms | 1.048 ms |    1 |  11.7188 |       - |      - |  118.89 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items | 2.026 ms | 0.0186 ms | 0.0165 ms | 2.022 ms |    2 |  15.6250 |       - |      - |  179.38 KB |
|                                Sessions_Medium | 5.139 ms | 0.1013 ms | 0.1693 ms | 5.102 ms |    3 | 101.5625 | 39.0625 | 7.8125 | 1092.98 KB |
|                                 Sessions_Large | 5.114 ms | 0.1017 ms | 0.1583 ms | 5.061 ms |    3 | 101.5625 | 39.0625 | 7.8125 | 1080.75 KB |
