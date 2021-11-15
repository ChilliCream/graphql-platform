``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=6.0.100
  [Host]     : .NET Core 5.0.12 (CoreCLR 5.0.1221.52207, CoreFX 5.0.1221.52207), X64 RyuJIT
  DefaultJob : .NET Core 5.0.12 (CoreCLR 5.0.1221.52207, CoreFX 5.0.1221.52207), X64 RyuJIT


```
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|-------:|------:|----------:|
|               Query_TypeName |   7.875 μs | 0.0726 μs | 0.0679 μs |   7.886 μs |    1 |  0.2289 |      - |     - |   2.38 KB |
|          Query_Introspection | 958.944 μs | 5.0395 μs | 4.7139 μs | 957.756 μs |    2 | 22.4609 |      - |     - | 230.44 KB |
| Query_Introspection_Prepared | 969.595 μs | 4.3669 μs | 4.0848 μs | 968.092 μs |    2 | 21.4844 | 0.9766 |     - |  229.1 KB |
