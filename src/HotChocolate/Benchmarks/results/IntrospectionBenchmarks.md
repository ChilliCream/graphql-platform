``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 12.0 (21A5506j) [Darwin 21.1.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.400
  [Host]     : .NET Core 5.0.9 (CoreCLR 5.0.921.35908, CoreFX 5.0.921.35908), X64 RyuJIT
  DefaultJob : .NET Core 5.0.9 (CoreCLR 5.0.921.35908, CoreFX 5.0.921.35908), X64 RyuJIT


```
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|-------:|------:|----------:|
|               Query_TypeName |   7.950 μs | 0.1333 μs | 0.1247 μs |   7.924 μs |    1 |  0.2594 |      - |     - |   2.77 KB |
|          Query_Introspection | 895.865 μs | 3.3316 μs | 3.1164 μs | 896.054 μs |    2 | 22.4609 | 0.9766 |     - | 230.96 KB |
| Query_Introspection_Prepared | 897.750 μs | 3.0161 μs | 2.6737 μs | 898.582 μs |    2 | 22.4609 | 0.9766 |     - | 229.63 KB |
