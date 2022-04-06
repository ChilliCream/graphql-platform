``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.3 (21E230) [Darwin 21.4.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT


```
|                          Method |     Mean |    Error |   StdDev |   Median | Rank |  Gen 0 |  Gen 1 | Allocated |
|-------------------------------- |---------:|---------:|---------:|---------:|-----:|-------:|-------:|----------:|
|      Introspection_Parse_String | 13.50 μs | 0.096 μs | 0.090 μs | 13.49 μs |    2 | 1.7242 | 0.1831 |     18 KB |
|       Introspection_Parse_Bytes | 13.21 μs | 0.064 μs | 0.060 μs | 13.22 μs |    1 | 1.7242 | 0.1831 |     18 KB |
| KitchenSink_Schema_Parse_String | 35.10 μs | 0.256 μs | 0.240 μs | 35.02 μs |    6 | 3.6621 | 0.7324 |     38 KB |
|  KitchenSink_Schema_Parse_Bytes | 34.33 μs | 0.252 μs | 0.223 μs | 34.25 μs |    5 | 3.6621 | 0.7324 |     38 KB |
|  KitchenSink_Query_Parse_String | 26.59 μs | 0.181 μs | 0.161 μs | 26.57 μs |    4 | 3.3264 | 0.6409 |     34 KB |
|   KitchenSink_Query_Parse_Bytes | 25.76 μs | 0.105 μs | 0.098 μs | 25.78 μs |    3 | 3.3264 | 0.6409 |     34 KB |

12.7.0
|                          Method |     Mean |    Error |   StdDev |   Median | Rank |  Gen 0 |  Gen 1 | Allocated |
|-------------------------------- |---------:|---------:|---------:|---------:|-----:|-------:|-------:|----------:|
|      Introspection_Parse_String | 13.51 μs | 0.064 μs | 0.053 μs | 13.51 μs |    2 | 1.7242 | 0.1831 |     18 KB |
|       Introspection_Parse_Bytes | 13.22 μs | 0.053 μs | 0.050 μs | 13.21 μs |    1 | 1.7242 | 0.1831 |     18 KB |
| KitchenSink_Schema_Parse_String | 35.18 μs | 0.283 μs | 0.251 μs | 35.19 μs |    4 | 3.7231 | 0.7935 |     39 KB |
|  KitchenSink_Schema_Parse_Bytes | 36.12 μs | 0.714 μs | 0.850 μs | 35.91 μs |    5 | 3.7231 | 0.7324 |     39 KB |
|  KitchenSink_Query_Parse_String | 25.91 μs | 0.227 μs | 0.201 μs | 25.90 μs |    3 | 3.3569 | 0.6714 |     34 KB |
|   KitchenSink_Query_Parse_Bytes | 25.69 μs | 0.158 μs | 0.148 μs | 25.69 μs |    3 | 3.3569 | 0.6409 |     34 KB |
