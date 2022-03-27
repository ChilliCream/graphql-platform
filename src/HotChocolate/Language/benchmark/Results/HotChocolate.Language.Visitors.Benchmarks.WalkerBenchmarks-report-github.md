``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.3 (21E230) [Darwin 21.4.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), Arm64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), Arm64 RyuJIT


```
|    Method |      Mean |     Error |    StdDev |    Median | Rank |  Gen 0 | Allocated |
|---------- |----------:|----------:|----------:|----------:|-----:|-------:|----------:|
| Walker_V1 |  4.307 μs | 0.0089 μs | 0.0084 μs |  4.304 μs |    2 | 6.6528 |  13,920 B |
| Walker_V2 | 32.630 μs | 0.0689 μs | 0.0610 μs | 32.638 μs |    3 | 9.4604 |  19,784 B |
| Walker_V3 |  2.535 μs | 0.0105 μs | 0.0098 μs |  2.535 μs |    1 |      - |         - |

Wip
|    Method |      Mean |     Error |    StdDev |    Median | Rank |  Gen 0 | Allocated |
|---------- |----------:|----------:|----------:|----------:|-----:|-------:|----------:|
| Walker_V1 |  4.264 μs | 0.0076 μs | 0.0064 μs |  4.265 μs |    2 | 6.6528 |  13,920 B |
| Walker_V2 | 32.527 μs | 0.0565 μs | 0.0472 μs | 32.540 μs |    3 | 9.4604 |  19,784 B |
| Walker_V3 |  2.523 μs | 0.0111 μs | 0.0104 μs |  2.521 μs |    1 |      - |         - |