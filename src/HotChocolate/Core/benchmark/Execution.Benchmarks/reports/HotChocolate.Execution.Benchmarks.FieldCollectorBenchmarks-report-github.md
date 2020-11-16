``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.7 (19H15) [Darwin 19.6.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.100-rc.2.20479.15
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.47505, CoreFX 5.0.20.47505), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.47505, CoreFX 5.0.20.47505), X64 RyuJIT


```
|                             Method |     Mean |    Error |   StdDev |   Median | Rank |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------------------- |---------:|---------:|---------:|---------:|-----:|-------:|-------:|------:|----------:|
| PrepareSelectionSets_Introspection | 29.19 μs | 0.154 μs | 0.136 μs | 29.21 μs |    2 | 2.5330 | 0.2136 |     - |  26.14 KB |
|      PrepareSelectionSets_StarWars | 19.88 μs | 0.128 μs | 0.114 μs | 19.86 μs |    1 | 1.8616 | 0.1221 |     - |  19.25 KB |
