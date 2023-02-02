``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.3 (21E230) [Darwin 21.4.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT


```
|                        Method |      Mean |     Error |    StdDev |    Median | Rank | Allocated |
|------------------------------ |----------:|----------:|----------:|----------:|-----:|----------:|
|      Introspection_Read_Bytes |  4.572 μs | 0.0286 μs | 0.0268 μs |  4.581 μs |    1 |         - |
| KitchenSink_Schema_Read_Bytes | 13.499 μs | 0.0739 μs | 0.0692 μs | 13.499 μs |    3 |         - |
|  KitchenSink_Query_Read_Bytes | 11.072 μs | 0.0693 μs | 0.0648 μs | 11.103 μs |    2 |         - |

12.7.0
|                        Method |      Mean |     Error |    StdDev |    Median | Rank | Allocated |
|------------------------------ |----------:|----------:|----------:|----------:|-----:|----------:|
|      Introspection_Read_Bytes |  4.616 μs | 0.0377 μs | 0.0334 μs |  4.616 μs |    1 |         - |
| KitchenSink_Schema_Read_Bytes | 12.516 μs | 0.1024 μs | 0.0958 μs | 12.509 μs |    3 |         - |
|  KitchenSink_Query_Read_Bytes | 10.503 μs | 0.0623 μs | 0.0583 μs | 10.487 μs |    2 |         - |

