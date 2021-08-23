``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 11.2.3 (20D91) [Darwin 20.3.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.103
  [Host]     : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  DefaultJob : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                Method |     Mean |   Error |  StdDev |   Median | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|--------:|--------:|---------:|-----:|-------:|------:|------:|----------:|
|    One_String_Literal | 144.5 ns | 2.60 ns | 3.72 ns | 142.8 ns |    1 |      - |     - |     - |         - |
|            One_String | 155.9 ns | 0.66 ns | 0.59 ns | 155.9 ns |    2 | 0.0052 |     - |     - |      56 B |
|    One_Object_Literal | 457.6 ns | 1.26 ns | 1.06 ns | 457.9 ns |    3 | 0.0143 |     - |     - |     152 B |
| One_Object_Dictionary | 565.6 ns | 2.96 ns | 2.31 ns | 565.7 ns |    4 | 0.0658 |     - |     - |     688 B |
