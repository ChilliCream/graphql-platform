``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.3 (21E230) [Darwin 21.4.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), Arm64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), Arm64 RyuJIT


```
|                        Method |     Mean |    Error |    StdDev |    Median | Rank | Allocated |
|------------------------------ |---------:|---------:|----------:|----------:|-----:|----------:|
|      Introspection_Read_Bytes | 15.97 μs | 0.909 μs |  2.679 μs | 17.006 μs |    1 |         - |
| KitchenSink_Schema_Read_Bytes | 38.16 μs | 5.332 μs | 15.721 μs | 42.637 μs |    3 |         - |
|  KitchenSink_Query_Read_Bytes | 22.08 μs | 6.475 μs | 19.093 μs |  7.456 μs |    2 |         - |

Wip
|                        Method |      Mean |    Error |    StdDev |    Median | Rank | Allocated |
|------------------------------ |----------:|---------:|----------:|----------:|-----:|----------:|
|      Introspection_Read_Bytes |  8.169 μs | 2.195 μs |  6.471 μs |  3.341 μs |    1 |         - |
| KitchenSink_Schema_Read_Bytes | 11.485 μs | 2.179 μs |  5.853 μs |  8.597 μs |    2 |         - |
|  KitchenSink_Query_Read_Bytes | 29.552 μs | 5.794 μs | 17.084 μs | 39.903 μs |    3 |         - |

|                        Method |     Mean |    Error |    StdDev |   Median | Rank | Allocated |
|------------------------------ |---------:|---------:|----------:|---------:|-----:|----------:|
|      Introspection_Read_Bytes | 16.74 μs | 1.269 μs |  3.742 μs | 17.91 μs |    1 |         - |
| KitchenSink_Schema_Read_Bytes | 38.48 μs | 7.038 μs | 20.751 μs | 46.45 μs |    3 |         - |
|  KitchenSink_Query_Read_Bytes | 36.19 μs | 5.575 μs | 16.438 μs | 43.50 μs |    2 |         - |
