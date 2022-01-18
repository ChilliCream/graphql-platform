``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19044
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=6.0.101
  [Host]     : .NET Core 6.0.1 (CoreCLR 6.0.121.56705, CoreFX 6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET Core 6.0.1 (CoreCLR 6.0.121.56705, CoreFX 6.0.121.56705), X64 RyuJIT


```
|                Method |     Mean |   Error |  StdDev |   Median | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|--------:|--------:|---------:|-----:|-------:|------:|------:|----------:|
| ValidateIntrospection | 118.3 μs | 1.25 μs | 1.10 μs | 118.6 μs |    2 | 0.2441 |     - |     - |   2.54 KB |
|      ValidateStarWars | 115.2 μs | 0.27 μs | 0.26 μs | 115.2 μs |    1 | 0.2441 |     - |     - |   2.54 KB |
