``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1466 (21H2)
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT


```
|                        Method |         Mean |     Error |    StdDev |       Median | Rank |  Gen 0 |  Gen 1 | Allocated |
|------------------------------ |-------------:|----------:|----------:|-------------:|-----:|-------:|-------:|----------:|
|          Create_ExtensionData |     2.589 ns | 0.0271 ns | 0.0226 ns |     2.595 ns |    2 | 0.0029 |      - |      24 B |
|        Create_ExtensionData_2 |     1.707 ns | 0.0227 ns | 0.0212 ns |     1.706 ns |    1 | 0.0029 |      - |      24 B |
|    Create_ExtensionData_Set_1 |    72.679 ns | 0.6837 ns | 0.6396 ns |    72.257 ns |    7 | 0.0153 |      - |     128 B |
|  Create_ExtensionData_2_Set_1 |    29.368 ns | 0.1376 ns | 0.1149 ns |    29.363 ns |    5 | 0.0287 |      - |     240 B |
|    Create_ExtensionData_Set_2 |   187.498 ns | 1.6950 ns | 1.5026 ns |   187.719 ns |    8 | 0.0410 |      - |     344 B |
|  Create_ExtensionData_2_Set_2 |    46.798 ns | 0.3243 ns | 0.3033 ns |    46.856 ns |    6 | 0.0344 |      - |     288 B |
|   Create_ExtensionData_Set_10 | 1,535.841 ns | 4.6301 ns | 4.3310 ns | 1,535.178 ns |   10 | 0.3147 | 0.0019 |   2,648 B |
| Create_ExtensionData_2_Set_10 |   229.478 ns | 1.1805 ns | 1.1042 ns |   229.666 ns |    9 | 0.1500 | 0.0010 |   1,256 B |
|    Create_ExtensionData_Get_1 |    11.472 ns | 0.0371 ns | 0.0329 ns |    11.476 ns |    4 |      - |      - |         - |
|  Create_ExtensionData_2_Get_1 |     8.100 ns | 0.0167 ns | 0.0140 ns |     8.099 ns |    3 |      - |      - |         - |
