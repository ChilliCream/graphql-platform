``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.6 (19G2021) [Darwin 19.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT


```
|                Method |     Mean |    Error |   StdDev |   Median | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|---------:|---------:|---------:|-----:|-------:|------:|------:|----------:|
|    One_String_Literal | 158.9 ns |  3.17 ns |  5.88 ns | 158.0 ns |    1 |      - |     - |     - |         - |
|            One_String | 188.0 ns |  3.73 ns |  6.73 ns | 187.9 ns |    2 | 0.0067 |     - |     - |      56 B |
|    One_Object_Literal | 547.6 ns | 10.95 ns | 28.46 ns | 543.9 ns |    3 | 0.0172 |     - |     - |     144 B |
| One_Object_Dictionary | 657.4 ns | 13.12 ns | 28.23 ns | 652.1 ns |    4 | 0.0763 |     - |     - |     640 B |
