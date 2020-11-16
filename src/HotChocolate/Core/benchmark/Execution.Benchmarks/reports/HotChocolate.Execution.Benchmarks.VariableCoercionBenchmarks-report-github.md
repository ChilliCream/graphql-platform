``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.7 (19H15) [Darwin 19.6.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.100-rc.2.20479.15
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.47505, CoreFX 5.0.20.47505), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.47505, CoreFX 5.0.20.47505), X64 RyuJIT


```
|                Method |     Mean |   Error |  StdDev |   Median | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|--------:|--------:|---------:|-----:|-------:|------:|------:|----------:|
|    One_String_Literal | 139.8 ns | 0.54 ns | 0.48 ns | 139.8 ns |    1 |      - |     - |     - |         - |
|            One_String | 156.6 ns | 0.40 ns | 0.38 ns | 156.5 ns |    2 | 0.0052 |     - |     - |      56 B |
|    One_Object_Literal | 478.6 ns | 4.42 ns | 3.69 ns | 479.0 ns |    3 | 0.0134 |     - |     - |     144 B |
| One_Object_Dictionary | 579.0 ns | 5.25 ns | 4.65 ns | 578.7 ns |    4 | 0.0610 |     - |     - |     640 B |
