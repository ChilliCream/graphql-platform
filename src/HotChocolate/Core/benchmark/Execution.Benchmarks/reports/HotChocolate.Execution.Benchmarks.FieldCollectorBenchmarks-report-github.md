``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.6 (19G2021) [Darwin 19.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT


```
|                             Method |     Mean |    Error |   StdDev |   Median | Rank |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------------------- |---------:|---------:|---------:|---------:|-----:|-------:|-------:|------:|----------:|
| PrepareSelectionSets_Introspection | 28.30 μs | 0.554 μs | 0.812 μs | 27.92 μs |    2 | 3.1128 | 0.2747 |     - |  25.63 KB |
|      PrepareSelectionSets_StarWars | 18.79 μs | 0.319 μs | 0.283 μs | 18.72 μs |    1 | 2.2278 | 0.1526 |     - |  18.34 KB |
