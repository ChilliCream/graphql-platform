``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1466 (21H2)
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT


```
|                       Method |       Mean |     Error |    StdDev |     Median | Rank |   Gen 0 |  Gen 1 | Allocated |
|----------------------------- |-----------:|----------:|----------:|-----------:|-----:|--------:|-------:|----------:|
|               Query_TypeName |   4.766 μs | 0.0306 μs | 0.0286 μs |   4.767 μs |    1 |  0.2594 |      - |      2 KB |
|          Query_Introspection | 730.073 μs | 6.9186 μs | 6.4717 μs | 730.246 μs |    3 | 27.3438 | 0.9766 |    230 KB |
| Query_Introspection_Prepared | 673.435 μs | 3.3553 μs | 2.9744 μs | 672.392 μs |    2 | 27.3438 | 0.9766 |    229 KB |
