``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.6 (19G2021) [Darwin 19.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT


```
|                     Method | Size |        Mean |      Error |     StdDev |      Median | Rank |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------- |----- |------------:|-----------:|-----------:|------------:|-----:|-------:|-------:|------:|----------:|
| **Create_And_Fill_Dictionary** |    **1** |    **62.11 ns** |   **1.310 ns** |   **2.676 ns** |    **61.55 ns** |    **3** | **0.0286** |      **-** |     **-** |     **240 B** |
|  Create_And_Fill_ArrayPool |    1 |    39.22 ns |   0.852 ns |   1.887 ns |    39.43 ns |    2 | 0.0029 |      - |     - |      24 B |
|  Create_And_Fill_ResultMap |    1 |    18.52 ns |   0.440 ns |   1.176 ns |    18.42 ns |    1 | 0.0029 |      - |     - |      24 B |
| **Create_And_Fill_Dictionary** |    **8** |   **376.28 ns** |   **8.069 ns** |  **23.410 ns** |   **376.55 ns** |    **8** | **0.1411** | **0.0005** |     **-** |    **1184 B** |
|  Create_And_Fill_ArrayPool |    8 |   133.85 ns |   2.827 ns |   8.336 ns |   134.46 ns |    5 | 0.0229 |      - |     - |     192 B |
|  Create_And_Fill_ResultMap |    8 |   120.87 ns |   2.167 ns |   2.027 ns |   120.72 ns |    4 | 0.0229 |      - |     - |     192 B |
| **Create_And_Fill_Dictionary** |   **16** |   **577.03 ns** |  **11.217 ns** |  **13.775 ns** |   **574.92 ns** |    **9** | **0.1869** | **0.0010** |     **-** |    **1568 B** |
|  Create_And_Fill_ArrayPool |   16 |   273.77 ns |   5.487 ns |  14.261 ns |   270.19 ns |    6 | 0.0687 |      - |     - |     576 B |
|  Create_And_Fill_ResultMap |   16 |   311.06 ns |   6.596 ns |  19.345 ns |   309.70 ns |    7 | 0.0687 |      - |     - |     576 B |
| **Create_And_Fill_Dictionary** |   **50** | **2,450.51 ns** |  **58.565 ns** | **172.681 ns** | **2,454.45 ns** |   **14** | **0.8469** | **0.0229** |     **-** |    **7104 B** |
|  Create_And_Fill_ArrayPool |   50 |   851.50 ns |  11.219 ns |   9.369 ns |   848.72 ns |   10 | 0.2956 | 0.0019 |     - |    2480 B |
|  Create_And_Fill_ResultMap |   50 |   988.84 ns |  16.332 ns |  13.638 ns |   984.89 ns |   11 | 0.2956 | 0.0010 |     - |    2480 B |
| **Create_And_Fill_Dictionary** |  **100** | **4,572.79 ns** |  **91.443 ns** | **128.190 ns** | **4,538.46 ns** |   **17** | **1.8463** | **0.1297** |     **-** |   **15472 B** |
|  Create_And_Fill_ArrayPool |  100 | 1,753.23 ns |  30.171 ns |  44.224 ns | 1,738.51 ns |   12 | 0.6294 | 0.0114 |     - |    5280 B |
|  Create_And_Fill_ResultMap |  100 | 1,932.22 ns |  32.374 ns |  27.033 ns | 1,927.01 ns |   13 | 0.6294 | 0.0038 |     - |    5280 B |
| **Create_And_Fill_Dictionary** |  **200** | **9,500.79 ns** | **186.221 ns** | **272.961 ns** | **9,437.14 ns** |   **18** | **3.9673** | **0.5341** |     **-** |   **33192 B** |
|  Create_And_Fill_ArrayPool |  200 | 3,798.98 ns |  73.924 ns | 121.460 ns | 3,766.87 ns |   15 | 1.3008 | 0.0496 |     - |   10880 B |
|  Create_And_Fill_ResultMap |  200 | 4,458.39 ns | 101.737 ns | 298.378 ns | 4,390.77 ns |   16 | 1.2970 | 0.0229 |     - |   10880 B |
