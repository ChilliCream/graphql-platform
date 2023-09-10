``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT


```

No String
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |   Gen 0 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|----------:|
|               Query_TypeName |   3.746 μs | 0.0207 μs | 0.0193 μs |   3.749 μs |    1 |  1.1520 |      2 KB |
|          Query_Introspection | 791.826 μs | 0.8260 μs | 0.7322 μs | 791.764 μs |    3 | 20.5078 |     44 KB |
| Query_Introspection_Prepared | 774.586 μs | 0.8447 μs | 0.7488 μs | 774.639 μs |    2 | 20.5078 |     42 KB |

12.2.1
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |    Gen 0 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|---------:|----------:|
|               Query_TypeName |   3.734 μs | 0.0586 μs | 0.0520 μs |   3.714 μs |    1 |   1.0834 |      2 KB |
|          Query_Introspection | 781.910 μs | 1.0410 μs | 0.9738 μs | 782.093 μs |    2 | 112.3047 |    230 KB |
| Query_Introspection_Prepared | 774.779 μs | 1.6926 μs | 1.5833 μs | 774.514 μs |    2 | 111.3281 |    229 KB |

12.0.1
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |    Gen 0 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|---------:|----------:|
|               Query_TypeName |   3.904 μs | 0.0093 μs | 0.0083 μs |   3.902 μs |    1 |   1.2054 |      2 KB |
|          Query_Introspection | 776.849 μs | 0.7285 μs | 0.6458 μs | 776.937 μs |    3 | 112.3047 |    231 KB |
| Query_Introspection_Prepared | 767.238 μs | 1.4716 μs | 1.3765 μs | 767.684 μs |    2 | 111.3281 |    229 KB |
