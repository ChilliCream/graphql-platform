``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.3 (21E230) [Darwin 21.4.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), Arm64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), Arm64 RyuJIT


```
|                          Method |     Mean |    Error |   StdDev |   Median | Rank |   Gen 0 | Allocated |
|-------------------------------- |---------:|---------:|---------:|---------:|-----:|--------:|----------:|
|      Introspection_Parse_String | 12.56 μs | 0.034 μs | 0.030 μs | 12.56 μs |    2 |  8.6365 |     18 KB |
|       Introspection_Parse_Bytes | 12.41 μs | 0.033 μs | 0.028 μs | 12.42 μs |    1 |  8.6365 |     18 KB |
| KitchenSink_Schema_Parse_String | 29.37 μs | 0.124 μs | 0.116 μs | 29.38 μs |    5 | 18.4326 |     38 KB |
|  KitchenSink_Schema_Parse_Bytes | 29.11 μs | 0.125 μs | 0.097 μs | 29.14 μs |    5 | 18.4326 |     38 KB |
|  KitchenSink_Query_Parse_String | 23.88 μs | 0.084 μs | 0.079 μs | 23.88 μs |    4 | 16.6931 |     34 KB |
|   KitchenSink_Query_Parse_Bytes | 23.18 μs | 0.057 μs | 0.051 μs | 23.18 μs |    3 | 16.6931 |     34 KB |


Wip
|                          Method |     Mean |    Error |   StdDev |   Median | Rank |   Gen 0 | Allocated |
|-------------------------------- |---------:|---------:|---------:|---------:|-----:|--------:|----------:|
|      Introspection_Parse_String | 12.52 μs | 0.037 μs | 0.031 μs | 12.51 μs |    1 |  8.6365 |     18 KB |
|       Introspection_Parse_Bytes | 12.47 μs | 0.043 μs | 0.038 μs | 12.47 μs |    1 |  8.6365 |     18 KB |
| KitchenSink_Schema_Parse_String | 28.72 μs | 0.099 μs | 0.088 μs | 28.69 μs |    3 | 18.4326 |     38 KB |
|  KitchenSink_Schema_Parse_Bytes | 29.09 μs | 0.139 μs | 0.123 μs | 29.09 μs |    4 | 18.4326 |     38 KB |
|  KitchenSink_Query_Parse_String | 23.14 μs | 0.061 μs | 0.051 μs | 23.13 μs |    2 | 16.6931 |     34 KB |
|   KitchenSink_Query_Parse_Bytes | 23.28 μs | 0.052 μs | 0.040 μs | 23.29 μs |    2 | 16.6931 |     34 KB |

|                          Method |     Mean |    Error |   StdDev |   Median | Rank |   Gen 0 | Allocated |
|-------------------------------- |---------:|---------:|---------:|---------:|-----:|--------:|----------:|
|      Introspection_Parse_String | 12.52 μs | 0.044 μs | 0.037 μs | 12.51 μs |    1 |  8.6365 |     18 KB |
|       Introspection_Parse_Bytes | 12.73 μs | 0.044 μs | 0.039 μs | 12.73 μs |    2 |  8.6365 |     18 KB |
| KitchenSink_Schema_Parse_String | 29.16 μs | 0.133 μs | 0.125 μs | 29.15 μs |    4 | 18.4326 |     38 KB |
|  KitchenSink_Schema_Parse_Bytes | 29.06 μs | 0.144 μs | 0.128 μs | 29.07 μs |    4 | 18.4326 |     38 KB |
|  KitchenSink_Query_Parse_String | 24.27 μs | 0.097 μs | 0.091 μs | 24.24 μs |    3 | 16.6931 |     34 KB |
|   KitchenSink_Query_Parse_Bytes | 24.10 μs | 0.070 μs | 0.066 μs | 24.08 μs |    3 | 16.6931 |     34 KB |