``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.7 (19H2) [Darwin 19.6.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT


```
|                             Method |     Mean |    Error |   StdDev |   Median | Rank |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------------------- |---------:|---------:|---------:|---------:|-----:|-------:|-------:|------:|----------:|
| PrepareSelectionSets_Introspection | 25.58 μs | 0.487 μs | 0.478 μs | 25.36 μs |    2 | 2.5024 | 0.2136 |     - |  25.63 KB |
|      PrepareSelectionSets_StarWars | 16.65 μs | 0.287 μs | 0.269 μs | 16.53 μs |    1 | 1.7700 | 0.1221 |     - |  18.34 KB |
