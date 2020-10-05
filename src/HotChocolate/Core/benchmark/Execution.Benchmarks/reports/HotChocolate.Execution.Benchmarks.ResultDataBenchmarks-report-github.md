``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.7 (19H2) [Darwin 19.6.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT


```
|                     Method | Size |        Mean |      Error |     StdDev |      Median | Rank |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|--------------------------- |----- |------------:|-----------:|-----------:|------------:|-----:|-------:|-------:|-------:|----------:|
| **Create_And_Fill_Dictionary** |    **1** |    **51.03 ns** |   **0.725 ns** |   **0.642 ns** |    **50.83 ns** |    **3** | **0.0229** |      **-** |      **-** |     **240 B** |
|  Create_And_Fill_ArrayPool |    1 |    30.06 ns |   0.604 ns |   0.827 ns |    29.70 ns |    2 | 0.0023 |      - |      - |      24 B |
|  Create_And_Fill_ResultMap |    1 |    19.94 ns |   0.222 ns |   0.186 ns |    19.95 ns |    1 | 0.0023 |      - |      - |      24 B |
| **Create_And_Fill_Dictionary** |    **8** |   **465.41 ns** |   **5.564 ns** |   **4.646 ns** |   **464.75 ns** |    **8** | **0.1130** | **0.0005** |      **-** |    **1184 B** |
|  Create_And_Fill_ArrayPool |    8 |   143.00 ns |   2.613 ns |   2.444 ns |   142.79 ns |    5 | 0.0184 |      - |      - |     192 B |
|  Create_And_Fill_ResultMap |    8 |    98.58 ns |   1.890 ns |   2.829 ns |    98.07 ns |    4 | 0.0184 |      - |      - |     192 B |
| **Create_And_Fill_Dictionary** |   **16** |   **498.96 ns** |  **10.026 ns** |  **10.728 ns** |   **496.39 ns** |    **9** | **0.1497** | **0.0010** |      **-** |    **1568 B** |
|  Create_And_Fill_ArrayPool |   16 |   213.14 ns |   3.217 ns |   3.010 ns |   212.12 ns |    6 | 0.0551 |      - |      - |     576 B |
|  Create_And_Fill_ResultMap |   16 |   231.26 ns |   2.787 ns |   2.327 ns |   231.36 ns |    7 | 0.0548 |      - |      - |     576 B |
| **Create_And_Fill_Dictionary** |   **50** | **2,029.05 ns** |  **17.202 ns** |  **15.249 ns** | **2,026.58 ns** |   **14** | **0.6790** | **0.0229** |      **-** |    **7104 B** |
|  Create_And_Fill_ArrayPool |   50 |   860.53 ns |  13.253 ns |  12.397 ns |   859.07 ns |   10 | 0.2365 | 0.0019 |      - |    2480 B |
|  Create_And_Fill_ResultMap |   50 |   921.01 ns |  17.578 ns |  19.538 ns |   919.34 ns |   11 | 0.2365 | 0.0010 |      - |    2480 B |
| **Create_And_Fill_Dictionary** |  **100** | **4,626.16 ns** |  **92.034 ns** | **102.296 ns** | **4,588.51 ns** |   **16** | **1.4725** | **0.1068** |      **-** |   **15472 B** |
|  Create_And_Fill_ArrayPool |  100 | 1,616.39 ns |  32.094 ns |  52.731 ns | 1,607.14 ns |   12 | 0.5035 | 0.0095 |      - |    5280 B |
|  Create_And_Fill_ResultMap |  100 | 1,631.37 ns |   7.813 ns |   6.100 ns | 1,631.72 ns |   13 | 0.5035 | 0.0038 | 0.0019 |    5280 B |
| **Create_And_Fill_Dictionary** |  **200** | **7,984.81 ns** | **108.261 ns** | **111.176 ns** | **7,987.00 ns** |   **17** | **3.1738** | **0.4425** |      **-** |   **33192 B** |
|  Create_And_Fill_ArrayPool |  200 | 3,165.21 ns |  62.366 ns |  55.286 ns | 3,167.34 ns |   15 | 1.0376 | 0.0381 |      - |   10880 B |
|  Create_And_Fill_ResultMap |  200 | 4,452.51 ns | 182.177 ns | 534.293 ns | 4,721.03 ns |   16 | 1.0376 | 0.0191 |      - |   10880 B |
