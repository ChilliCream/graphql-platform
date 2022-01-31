``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1466 (21H2)
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT


```
|                              Method |      Mean |    Error |   StdDev |    Median | Rank |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|------------------------------------ |----------:|---------:|---------:|----------:|-----:|-----------:|----------:|----------:|----------:|
| CreateSchema_AnnotationBased_Medium |  63.40 ms | 0.220 ms | 0.183 ms |  63.38 ms |    2 |  2000.0000 |  857.1429 |         - |     16 MB |
|     CreateSchema_SchemaFirst_Medium | 263.13 ms | 2.883 ms | 2.697 ms | 262.93 ms |    3 | 15000.0000 | 5000.0000 | 2000.0000 |    126 MB |
|      CreateSchema_SchemaFirst_Large |  11.53 ms | 0.049 ms | 0.046 ms |  11.53 ms |    1 |   781.2500 |  375.0000 |         - |      6 MB |
