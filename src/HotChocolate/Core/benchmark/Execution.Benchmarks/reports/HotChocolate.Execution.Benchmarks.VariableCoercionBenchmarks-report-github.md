``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.7 (19H2) [Darwin 19.6.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT


```
|                Method |     Mean |    Error |   StdDev |   Median | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|---------:|---------:|---------:|-----:|-------:|------:|------:|----------:|
|    One_String_Literal | 165.0 ns |  2.18 ns |  2.04 ns | 165.0 ns |    1 |      - |     - |     - |         - |
|            One_String | 184.8 ns |  2.60 ns |  2.43 ns | 184.8 ns |    2 | 0.0052 |     - |     - |      56 B |
|    One_Object_Literal | 546.5 ns | 10.40 ns | 12.39 ns | 546.8 ns |    3 | 0.0134 |     - |     - |     144 B |
| One_Object_Dictionary | 693.9 ns | 10.07 ns |  9.42 ns | 691.5 ns |    4 | 0.0610 |     - |     - |     640 B |
