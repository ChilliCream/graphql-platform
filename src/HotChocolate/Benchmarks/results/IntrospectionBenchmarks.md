``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 5.0.12 (5.0.1221.52207), X64 RyuJIT
  DefaultJob : .NET 5.0.12 (5.0.1221.52207), X64 RyuJIT


```
main
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |   Gen 0 |  Gen 1 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|-------:|----------:|
|               Query_TypeName |   8.134 μs | 0.1533 μs | 0.1434 μs |   8.138 μs |    1 |  0.2289 |      - |      2 KB |
|          Query_Introspection | 960.094 μs | 5.8602 μs | 5.4817 μs | 961.218 μs |    2 | 22.4609 | 0.9766 |    230 KB |
| Query_Introspection_Prepared | 968.765 μs | 9.4212 μs | 8.8126 μs | 968.530 μs |    2 | 21.4844 | 0.9766 |    229 KB |

12.2.1
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |   Gen 0 |  Gen 1 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|-------:|----------:|
|               Query_TypeName |   8.100 μs | 0.0869 μs | 0.0770 μs |   8.104 μs |    1 |  0.2289 |      - |      2 KB |
|          Query_Introspection | 969.357 μs | 2.5649 μs | 2.3992 μs | 969.569 μs |    2 | 22.4609 | 0.9766 |    230 KB |
| Query_Introspection_Prepared | 958.411 μs | 3.8217 μs | 3.5748 μs | 958.212 μs |    2 | 21.4844 | 0.9766 |    229 KB |

12.0.1
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |   Gen 0 |  Gen 1 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|-------:|----------:|
|               Query_TypeName |   8.062 μs | 0.0536 μs | 0.0502 μs |   8.080 μs |    1 |  0.2594 |      - |      3 KB |
|          Query_Introspection | 951.355 μs | 4.8614 μs | 4.3095 μs | 950.287 μs |    2 | 22.4609 | 0.9766 |    231 KB |
| Query_Introspection_Prepared | 960.894 μs | 4.5215 μs | 4.2294 μs | 961.325 μs |    2 | 22.4609 | 0.9766 |    229 KB |
