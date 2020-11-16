``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.7 (19H15) [Darwin 19.6.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.100-rc.2.20479.15
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.47505, CoreFX 5.0.20.47505), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.47505, CoreFX 5.0.20.47505), X64 RyuJIT


```
|                     Method | Size |        Mean |     Error |    StdDev |      Median | Rank |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------- |----- |------------:|----------:|----------:|------------:|-----:|-------:|-------:|------:|----------:|
| **Create_And_Fill_Dictionary** |    **1** |    **52.43 ns** |  **0.153 ns** |  **0.136 ns** |    **52.43 ns** |    **3** | **0.0229** |      **-** |     **-** |     **240 B** |
|  Create_And_Fill_ArrayPool |    1 |    29.74 ns |  0.270 ns |  0.239 ns |    29.82 ns |    2 | 0.0023 |      - |     - |      24 B |
|  Create_And_Fill_ResultMap |    1 |    14.82 ns |  0.305 ns |  0.286 ns |    14.69 ns |    1 | 0.0023 |      - |     - |      24 B |
| **Create_And_Fill_Dictionary** |    **8** |   **320.14 ns** |  **2.100 ns** |  **1.753 ns** |   **320.61 ns** |    **8** | **0.1130** |      **-** |     **-** |    **1184 B** |
|  Create_And_Fill_ArrayPool |    8 |   103.81 ns |  1.080 ns |  1.010 ns |   104.11 ns |    5 | 0.0184 |      - |     - |     192 B |
|  Create_And_Fill_ResultMap |    8 |   102.09 ns |  0.265 ns |  0.234 ns |   102.08 ns |    4 | 0.0184 |      - |     - |     192 B |
| **Create_And_Fill_Dictionary** |   **16** |   **542.58 ns** |  **5.048 ns** |  **4.475 ns** |   **542.10 ns** |    **9** | **0.1497** | **0.0010** |     **-** |    **1568 B** |
|  Create_And_Fill_ArrayPool |   16 |   227.88 ns |  1.363 ns |  1.208 ns |   227.85 ns |    6 | 0.0551 |      - |     - |     576 B |
|  Create_And_Fill_ResultMap |   16 |   242.44 ns |  1.279 ns |  1.196 ns |   242.32 ns |    7 | 0.0548 |      - |     - |     576 B |
| **Create_And_Fill_Dictionary** |   **50** | **2,149.02 ns** | **21.885 ns** | **20.471 ns** | **2,145.86 ns** |   **14** | **0.6790** | **0.0229** |     **-** |    **7104 B** |
|  Create_And_Fill_ArrayPool |   50 |   746.83 ns |  4.849 ns |  4.535 ns |   747.37 ns |   10 | 0.2365 | 0.0019 |     - |    2480 B |
|  Create_And_Fill_ResultMap |   50 |   828.53 ns |  6.307 ns |  4.924 ns |   827.43 ns |   11 | 0.2365 |      - |     - |    2480 B |
| **Create_And_Fill_Dictionary** |  **100** | **4,228.38 ns** | **75.276 ns** | **70.413 ns** | **4,205.91 ns** |   **17** | **1.4725** | **0.1068** |     **-** |   **15472 B** |
|  Create_And_Fill_ArrayPool |  100 | 1,556.68 ns | 30.235 ns | 31.049 ns | 1,566.25 ns |   12 | 0.5035 | 0.0095 |     - |    5280 B |
|  Create_And_Fill_ResultMap |  100 | 1,706.82 ns | 16.799 ns | 14.892 ns | 1,705.31 ns |   13 | 0.5035 | 0.0038 |     - |    5280 B |
| **Create_And_Fill_Dictionary** |  **200** | **8,382.26 ns** | **68.324 ns** | **60.567 ns** | **8,376.07 ns** |   **18** | **3.1738** | **0.4425** |     **-** |   **33192 B** |
|  Create_And_Fill_ArrayPool |  200 | 3,367.62 ns | 25.932 ns | 22.988 ns | 3,367.25 ns |   15 | 1.0376 | 0.0381 |     - |   10880 B |
|  Create_And_Fill_ResultMap |  200 | 3,504.05 ns | 65.722 ns | 61.477 ns | 3,514.07 ns |   16 | 1.0376 | 0.0191 |     - |   10880 B |
