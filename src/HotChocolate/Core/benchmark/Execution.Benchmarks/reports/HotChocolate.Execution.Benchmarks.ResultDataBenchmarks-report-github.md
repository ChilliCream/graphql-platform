``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 11.2.3 (20D91) [Darwin 20.3.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.103
  [Host]     : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  DefaultJob : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                     Method | Size |        Mean |      Error |    StdDev |      Median | Rank |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------- |----- |------------:|-----------:|----------:|------------:|-----:|-------:|-------:|------:|----------:|
| **Create_And_Fill_Dictionary** |    **1** |    **58.07 ns** |   **0.819 ns** |  **0.766 ns** |    **57.92 ns** |    **3** | **0.0229** |      **-** |     **-** |     **240 B** |
|  Create_And_Fill_ArrayPool |    1 |    31.62 ns |   0.110 ns |  0.103 ns |    31.63 ns |    2 | 0.0023 |      - |     - |      24 B |
|  Create_And_Fill_ResultMap |    1 |    15.76 ns |   0.113 ns |  0.105 ns |    15.73 ns |    1 | 0.0023 |      - |     - |      24 B |
| **Create_And_Fill_Dictionary** |    **8** |   **340.36 ns** |   **2.686 ns** |  **2.513 ns** |   **340.42 ns** |    **7** | **0.1130** | **0.0005** |     **-** |    **1184 B** |
|  Create_And_Fill_ArrayPool |    8 |   111.04 ns |   0.832 ns |  0.779 ns |   110.97 ns |    4 | 0.0184 |      - |     - |     192 B |
|  Create_And_Fill_ResultMap |    8 |   110.29 ns |   0.346 ns |  0.323 ns |   110.34 ns |    4 | 0.0184 |      - |     - |     192 B |
| **Create_And_Fill_Dictionary** |   **16** |   **577.12 ns** |   **6.428 ns** |  **6.013 ns** |   **575.57 ns** |    **8** | **0.1497** | **0.0010** |     **-** |    **1568 B** |
|  Create_And_Fill_ArrayPool |   16 |   245.34 ns |   1.077 ns |  0.955 ns |   245.55 ns |    5 | 0.0548 |      - |     - |     576 B |
|  Create_And_Fill_ResultMap |   16 |   266.83 ns |   2.039 ns |  1.807 ns |   266.97 ns |    6 | 0.0548 |      - |     - |     576 B |
| **Create_And_Fill_Dictionary** |   **50** | **2,275.55 ns** |  **16.905 ns** | **14.986 ns** | **2,276.96 ns** |   **13** | **0.6790** | **0.0229** |     **-** |    **7104 B** |
|  Create_And_Fill_ArrayPool |   50 |   828.69 ns |  16.400 ns | 26.482 ns |   832.54 ns |    9 | 0.2365 | 0.0019 |     - |    2480 B |
|  Create_And_Fill_ResultMap |   50 |   930.92 ns |  17.525 ns | 22.163 ns |   925.12 ns |   10 | 0.2365 |      - |     - |    2480 B |
| **Create_And_Fill_Dictionary** |  **100** | **4,582.92 ns** |  **48.655 ns** | **45.512 ns** | **4,589.86 ns** |   **16** | **1.4725** | **0.1068** |     **-** |   **15472 B** |
|  Create_And_Fill_ArrayPool |  100 | 1,555.46 ns |   9.892 ns |  8.769 ns | 1,553.91 ns |   11 | 0.5035 | 0.0095 |     - |    5280 B |
|  Create_And_Fill_ResultMap |  100 | 1,843.95 ns |  18.783 ns | 17.569 ns | 1,841.02 ns |   12 | 0.5035 | 0.0038 |     - |    5280 B |
| **Create_And_Fill_Dictionary** |  **200** | **9,218.45 ns** | **106.341 ns** | **99.471 ns** | **9,229.71 ns** |   **17** | **3.1738** | **0.4425** |     **-** |   **33192 B** |
|  Create_And_Fill_ArrayPool |  200 | 3,284.55 ns |  41.825 ns | 37.077 ns | 3,289.38 ns |   14 | 1.0376 | 0.0381 |     - |   10880 B |
|  Create_And_Fill_ResultMap |  200 | 3,874.70 ns |  38.194 ns | 35.726 ns | 3,869.86 ns |   15 | 1.0376 | 0.0153 |     - |   10880 B |
