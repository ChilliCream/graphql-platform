``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.3 (21E230) [Darwin 21.4.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT


```
|    Method |      Mean |     Error |    StdDev |    Median | Rank |  Gen 0 | Allocated |
|---------- |----------:|----------:|----------:|----------:|-----:|-------:|----------:|
| Walker_V1 |  4.758 μs | 0.0238 μs | 0.0199 μs |  4.759 μs |    2 | 1.3275 |  13,920 B |
| Walker_V2 | 25.977 μs | 0.1682 μs | 0.1404 μs | 26.006 μs |    3 | 1.8921 |  19,784 B |
| Walker_V3 |  2.923 μs | 0.0284 μs | 0.0266 μs |  2.929 μs |    1 |      - |         - |

12.7.0
|    Method |      Mean |     Error |    StdDev |    Median | Rank |  Gen 0 | Allocated |
|---------- |----------:|----------:|----------:|----------:|-----:|-------:|----------:|
| Walker_V1 |  4.762 μs | 0.0192 μs | 0.0180 μs |  4.763 μs |    2 | 1.3275 |  13,920 B |
| Walker_V2 | 25.672 μs | 0.1535 μs | 0.1435 μs | 25.673 μs |    3 | 1.8921 |  19,784 B |
| Walker_V3 |  2.867 μs | 0.0086 μs | 0.0072 μs |  2.866 μs |    1 |      - |         - |
