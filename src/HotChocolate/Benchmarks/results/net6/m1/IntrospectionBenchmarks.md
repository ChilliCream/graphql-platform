``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT


```

12.0.1
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |    Gen 0 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|---------:|----------:|
|               Query_TypeName |   3.904 μs | 0.0093 μs | 0.0083 μs |   3.902 μs |    1 |   1.2054 |      2 KB |
|          Query_Introspection | 776.849 μs | 0.7285 μs | 0.6458 μs | 776.937 μs |    3 | 112.3047 |    231 KB |
| Query_Introspection_Prepared | 767.238 μs | 1.4716 μs | 1.3765 μs | 767.684 μs |    2 | 111.3281 |    229 KB |
