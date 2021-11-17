``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.1 (21C5031d) [Darwin 21.2.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |   Gen 0 |  Gen 1 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|-------:|----------:|
|               Query_TypeName |   7.537 μs | 0.0642 μs | 0.0601 μs |   7.530 μs |    1 |  0.2060 |      - |      2 KB |
|          Query_Introspection | 948.266 μs | 2.8120 μs | 2.3482 μs | 947.675 μs |    3 | 22.4609 | 0.9766 |    230 KB |
| Query_Introspection_Prepared | 918.987 μs | 2.5415 μs | 2.3773 μs | 919.450 μs |    2 | 21.4844 | 0.9766 |    229 KB |
