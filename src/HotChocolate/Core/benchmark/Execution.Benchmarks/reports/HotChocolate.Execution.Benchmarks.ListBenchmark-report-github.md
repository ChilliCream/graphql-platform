``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 11.2.3 (20D91) [Darwin 20.3.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.103
  [Host]     : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  DefaultJob : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                 Method | Size |      Mean |     Error |    StdDev |    Median | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------- |----- |----------:|----------:|----------:|----------:|-----:|-------:|------:|------:|----------:|
|              **ListOfInt** |    **0** |  **6.701 ns** | **0.1903 ns** | **0.2907 ns** |  **6.786 ns** |    **8** | **0.0031** |     **-** |     **-** |      **32 B** |
|           ListOfString |    0 |  5.602 ns | 0.1728 ns | 0.2981 ns |  5.631 ns |    5 | 0.0031 |     - |     - |      32 B |
|     OptimizedListOfInt |    0 |  4.998 ns | 0.1607 ns | 0.4062 ns |  5.073 ns |    4 | 0.0038 |     - |     - |      40 B |
|  OptimizedListOfString |    0 |  4.880 ns | 0.1647 ns | 0.4161 ns |  4.931 ns |    4 | 0.0046 |     - |     - |      48 B |
|    OptimizedListOfInt2 |    0 |  4.695 ns | 0.1545 ns | 0.2785 ns |  4.686 ns |    4 | 0.0031 |     - |     - |      32 B |
| OptimizedListOfString2 |    0 |  4.780 ns | 0.1523 ns | 0.4145 ns |  4.579 ns |    4 | 0.0038 |     - |     - |      40 B |
|    OptimizedListOfInt3 |    0 |  9.448 ns | 0.2490 ns | 0.7343 ns |  9.516 ns |   10 | 0.0053 |     - |     - |      56 B |
| OptimizedListOfString3 |    0 |  9.586 ns | 0.2488 ns | 0.6103 ns |  9.689 ns |   10 | 0.0053 |     - |     - |      56 B |
|    OptimizedListOfInt4 |    0 |  4.522 ns | 0.0562 ns | 0.0526 ns |  4.521 ns |    3 | 0.0046 |     - |     - |      48 B |
| OptimizedListOfString4 |    0 |  4.676 ns | 0.1190 ns | 0.1371 ns |  4.649 ns |    4 | 0.0046 |     - |     - |      48 B |
|          ListListOfInt |    0 |  3.673 ns | 0.1300 ns | 0.3285 ns |  3.616 ns |    2 | 0.0023 |     - |     - |      24 B |
|      ListListOfString4 |    0 |  3.453 ns | 0.1251 ns | 0.1834 ns |  3.365 ns |    1 | 0.0023 |     - |     - |      24 B |
|              **ListOfInt** |    **1** | **16.510 ns** | **0.1017 ns** | **0.0902 ns** | **16.525 ns** |   **15** | **0.0069** |     **-** |     **-** |      **72 B** |
|           ListOfString |    1 | 20.583 ns | 0.4716 ns | 1.1023 ns | 20.528 ns |   20 | 0.0084 |     - |     - |      88 B |
|     OptimizedListOfInt |    1 |  6.303 ns | 0.1840 ns | 0.3410 ns |  6.295 ns |    7 | 0.0038 |     - |     - |      40 B |
|  OptimizedListOfString |    1 |  7.449 ns | 0.2093 ns | 0.4414 ns |  7.524 ns |    9 | 0.0046 |     - |     - |      48 B |
|    OptimizedListOfInt2 |    1 |  6.056 ns | 0.1787 ns | 0.4069 ns |  6.132 ns |    6 | 0.0031 |     - |     - |      32 B |
| OptimizedListOfString2 |    1 |  7.375 ns | 0.1954 ns | 0.4528 ns |  7.376 ns |    9 | 0.0038 |     - |     - |      40 B |
|    OptimizedListOfInt3 |    1 |        NA |        NA |        NA |        NA |    ? |      - |     - |     - |         - |
| OptimizedListOfString3 |    1 |        NA |        NA |        NA |        NA |    ? |      - |     - |     - |         - |
|    OptimizedListOfInt4 |    1 | 11.155 ns | 0.2791 ns | 0.5702 ns | 11.212 ns |   12 | 0.0069 |     - |     - |      72 B |
| OptimizedListOfString4 |    1 | 12.689 ns | 0.1611 ns | 0.1582 ns | 12.691 ns |   13 | 0.0069 |     - |     - |      72 B |
|          ListListOfInt |    1 | 21.454 ns | 0.2014 ns | 0.1884 ns | 21.477 ns |   21 | 0.0092 |     - |     - |      96 B |
|      ListListOfString4 |    1 | 23.801 ns | 0.4508 ns | 0.4428 ns | 23.681 ns |   23 | 0.0107 |     - |     - |     112 B |
|              **ListOfInt** |    **2** | **17.843 ns** | **0.1837 ns** | **0.1718 ns** | **17.897 ns** |   **19** | **0.0069** |     **-** |     **-** |      **72 B** |
|           ListOfString |    2 | 22.524 ns | 0.1675 ns | 0.1567 ns | 22.532 ns |   22 | 0.0084 |     - |     - |      88 B |
|     OptimizedListOfInt |    2 |  7.694 ns | 0.0955 ns | 0.0894 ns |  7.697 ns |    9 | 0.0038 |     - |     - |      40 B |
|  OptimizedListOfString |    2 | 10.050 ns | 0.0577 ns | 0.0540 ns | 10.061 ns |   11 | 0.0046 |     - |     - |      48 B |
|    OptimizedListOfInt2 |    2 | 13.633 ns | 0.1358 ns | 0.1270 ns | 13.678 ns |   14 | 0.0069 |     - |     - |      72 B |
| OptimizedListOfString2 |    2 | 20.828 ns | 0.0899 ns | 0.0797 ns | 20.818 ns |   20 | 0.0092 |     - |     - |      96 B |
|    OptimizedListOfInt3 |    2 |        NA |        NA |        NA |        NA |    ? |      - |     - |     - |         - |
| OptimizedListOfString3 |    2 |        NA |        NA |        NA |        NA |    ? |      - |     - |     - |         - |
|    OptimizedListOfInt4 |    2 | 17.470 ns | 0.1276 ns | 0.1193 ns | 17.461 ns |   18 | 0.0092 |     - |     - |      96 B |
| OptimizedListOfString4 |    2 | 20.000 ns | 0.1684 ns | 0.1315 ns | 19.977 ns |   20 | 0.0092 |     - |     - |      96 B |
|          ListListOfInt |    2 | 25.630 ns | 0.5642 ns | 1.2735 ns | 25.401 ns |   24 | 0.0092 |     - |     - |      96 B |
|      ListListOfString4 |    2 | 28.870 ns | 0.4421 ns | 0.3919 ns | 28.742 ns |   26 | 0.0107 |     - |     - |     112 B |
|              **ListOfInt** |    **3** | **20.405 ns** | **0.4624 ns** | **1.0531 ns** | **20.196 ns** |   **20** | **0.0069** |     **-** |     **-** |      **72 B** |
|           ListOfString |    3 | 26.127 ns | 0.1975 ns | 0.1649 ns | 26.171 ns |   24 | 0.0084 |     - |     - |      88 B |
|     OptimizedListOfInt |    3 | 16.806 ns | 0.1273 ns | 0.1129 ns | 16.811 ns |   16 | 0.0076 |     - |     - |      80 B |
|  OptimizedListOfString |    3 | 27.755 ns | 0.2297 ns | 0.2149 ns | 27.832 ns |   26 | 0.0099 |     - |     - |     104 B |
|    OptimizedListOfInt2 |    3 | 17.065 ns | 0.1499 ns | 0.1329 ns | 17.099 ns |   17 | 0.0069 |     - |     - |      72 B |
| OptimizedListOfString2 |    3 | 27.397 ns | 0.2675 ns | 0.2502 ns | 27.395 ns |   26 | 0.0092 |     - |     - |      96 B |
|    OptimizedListOfInt3 |    3 |        NA |        NA |        NA |        NA |    ? |      - |     - |     - |         - |
| OptimizedListOfString3 |    3 |        NA |        NA |        NA |        NA |    ? |      - |     - |     - |         - |
|    OptimizedListOfInt4 |    3 | 26.833 ns | 0.2099 ns | 0.1963 ns | 26.861 ns |   25 | 0.0130 |     - |     - |     136 B |
| OptimizedListOfString4 |    3 | 36.834 ns | 0.3131 ns | 0.2615 ns | 36.778 ns |   28 | 0.0145 |     - |     - |     152 B |
|          ListListOfInt |    3 | 28.243 ns | 0.6229 ns | 1.0910 ns | 27.989 ns |   26 | 0.0092 |     - |     - |      96 B |
|      ListListOfString4 |    3 | 36.119 ns | 0.7796 ns | 1.5570 ns | 35.890 ns |   28 | 0.0107 |     - |     - |     112 B |
|              **ListOfInt** |    **4** | **22.449 ns** | **0.1739 ns** | **0.1627 ns** | **22.433 ns** |   **22** | **0.0069** |     **-** |     **-** |      **72 B** |
|           ListOfString |    4 | 32.812 ns | 0.2371 ns | 0.2102 ns | 32.828 ns |   27 | 0.0084 |     - |     - |      88 B |
|     OptimizedListOfInt |    4 | 22.384 ns | 0.1187 ns | 0.1052 ns | 22.338 ns |   22 | 0.0076 |     - |     - |      80 B |
|  OptimizedListOfString |    4 | 36.964 ns | 0.3325 ns | 0.2777 ns | 36.926 ns |   28 | 0.0099 |     - |     - |     104 B |
|    OptimizedListOfInt2 |    4 | 21.644 ns | 0.1428 ns | 0.1266 ns | 21.577 ns |   21 | 0.0069 |     - |     - |      72 B |
| OptimizedListOfString2 |    4 | 35.133 ns | 0.1971 ns | 0.1844 ns | 35.143 ns |   28 | 0.0092 |     - |     - |      96 B |
|    OptimizedListOfInt3 |    4 |        NA |        NA |        NA |        NA |    ? |      - |     - |     - |         - |
| OptimizedListOfString3 |    4 |        NA |        NA |        NA |        NA |    ? |      - |     - |     - |         - |
|    OptimizedListOfInt4 |    4 | 36.427 ns | 0.3305 ns | 0.3091 ns | 36.420 ns |   28 | 0.0130 |     - |     - |     136 B |
| OptimizedListOfString4 |    4 | 49.745 ns | 0.4998 ns | 0.4675 ns | 49.626 ns |   30 | 0.0145 |     - |     - |     152 B |
|          ListListOfInt |    4 | 28.791 ns | 0.3447 ns | 0.3224 ns | 28.729 ns |   26 | 0.0092 |     - |     - |      96 B |
|      ListListOfString4 |    4 | 38.360 ns | 0.1820 ns | 0.1614 ns | 38.412 ns |   29 | 0.0107 |     - |     - |     112 B |

Benchmarks with issues:
  ListBenchmark.OptimizedListOfInt3: DefaultJob [Size=1]
  ListBenchmark.OptimizedListOfString3: DefaultJob [Size=1]
  ListBenchmark.OptimizedListOfInt3: DefaultJob [Size=2]
  ListBenchmark.OptimizedListOfString3: DefaultJob [Size=2]
  ListBenchmark.OptimizedListOfInt3: DefaultJob [Size=3]
  ListBenchmark.OptimizedListOfString3: DefaultJob [Size=3]
  ListBenchmark.OptimizedListOfInt3: DefaultJob [Size=4]
  ListBenchmark.OptimizedListOfString3: DefaultJob [Size=4]
