``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 12.0 (21A5506j) [Darwin 21.1.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.400
  [Host]     : .NET Core 5.0.9 (CoreCLR 5.0.921.35908, CoreFX 5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET Core 5.0.9 (CoreCLR 5.0.921.35908, CoreFX 5.0.921.35908), X64 RyuJIT


```
|                                         Method |       Mean |    Error |    StdDev |     Median | Rank |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------------------------------------- |-----------:|---------:|----------:|-----------:|-----:|--------:|--------:|------:|----------:|
|             Sessions_TitleAndAbstract_10_Items |   806.1 μs |  7.01 μs |   5.86 μs |   808.1 μs |    1 |  5.8594 |       - |     - |  60.62 KB |
| Sessions_TitleAndAbstractAndTrackName_10_Items | 1,900.6 μs | 26.74 μs |  23.70 μs | 1,895.8 μs |    2 | 11.7188 |       - |     - | 120.33 KB |
|                                Sessions_Medium | 4,576.6 μs | 74.65 μs |  79.87 μs | 4,563.7 μs |    3 | 70.3125 | 23.4375 |     - | 754.28 KB |
|                                 Sessions_Large | 4,758.0 μs | 93.40 μs | 179.95 μs | 4,765.9 μs |    4 | 70.3125 | 23.4375 |     - | 754.23 KB |
