# Execution.Benchmarks

## ResultDataBenchmarks

|                     Method | Size |         Mean |      Error |     StdDev |       Median | Rank |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------- |----- |-------------:|-----------:|-----------:|-------------:|-----:|-------:|-------:|------:|----------:|
| Create_And_Fill_Dictionary |    1 |    123.11 ns |   1.037 ns |   0.919 ns |    122.95 ns |    3 | 0.0553 |      - |     - |     232 B |
|  Create_And_Fill_ArrayPool |    1 |     55.21 ns |   0.444 ns |   0.415 ns |     55.17 ns |    2 | 0.0057 |      - |     - |      24 B |
|  Create_And_Fill_ResultMap |    1 |     26.72 ns |   0.315 ns |   0.263 ns |     26.70 ns |    1 | 0.0057 |      - |     - |      24 B |
| Create_And_Fill_Dictionary |    8 |    580.61 ns |   9.960 ns |   7.776 ns |    577.74 ns |    8 | 0.2804 |      - |     - |    1176 B |
|  Create_And_Fill_ArrayPool |    8 |    223.30 ns |   2.931 ns |   2.598 ns |    221.90 ns |    5 | 0.0458 |      - |     - |     192 B |
|  Create_And_Fill_ResultMap |    8 |    201.18 ns |   1.521 ns |   1.348 ns |    201.08 ns |    4 | 0.0458 |      - |     - |     192 B |
| Create_And_Fill_Dictionary |   16 |    953.52 ns |  10.586 ns |   9.902 ns |    954.03 ns |    9 | 0.3719 |      - |     - |    1560 B |
|  Create_And_Fill_ArrayPool |   16 |    445.08 ns |   2.478 ns |   2.197 ns |    444.99 ns |    6 | 0.1373 |      - |     - |     576 B |
|  Create_And_Fill_ResultMap |   16 |    455.76 ns |   6.594 ns |   6.168 ns |    455.47 ns |    7 | 0.1373 |      - |     - |     576 B |
| Create_And_Fill_Dictionary |   50 |  3,362.83 ns |  39.987 ns |  37.404 ns |  3,357.03 ns |   14 | 1.6937 |      - |     - |    7096 B |
|  Create_And_Fill_ArrayPool |   50 |  1,423.16 ns |  19.212 ns |  17.971 ns |  1,414.99 ns |   10 | 0.5913 |      - |     - |    2480 B |
|  Create_And_Fill_ResultMap |   50 |  1,508.40 ns |  22.305 ns |  20.864 ns |  1,508.74 ns |   11 | 0.5913 |      - |     - |    2480 B |
| Create_And_Fill_Dictionary |  100 |  6,713.85 ns |  83.459 ns |  78.068 ns |  6,705.20 ns |   16 | 3.6926 |      - |     - |   15464 B |
|  Create_And_Fill_ArrayPool |  100 |  2,873.55 ns |  39.002 ns |  36.482 ns |  2,876.24 ns |   12 | 1.2589 |      - |     - |    5280 B |
|  Create_And_Fill_ResultMap |  100 |  3,199.01 ns |  34.064 ns |  31.864 ns |  3,194.94 ns |   13 | 1.2589 |      - |     - |    5280 B |
| Create_And_Fill_Dictionary |  200 | 13,966.98 ns | 278.893 ns | 640.803 ns | 13,654.90 ns |   17 | 7.9346 | 0.0153 |     - |   33184 B |
|  Create_And_Fill_ArrayPool |  200 |  5,955.16 ns |  93.907 ns |  83.246 ns |  5,963.67 ns |   15 | 2.6016 |      - |     - |   10880 B |
|  Create_And_Fill_ResultMap |  200 |  6,648.03 ns |  41.498 ns |  38.817 ns |  6,639.08 ns |   16 | 2.6016 |      - |     - |   10880 B |

## VariableCoercionBenchmarks

|                Method |     Mean |   Error |  StdDev |   Median | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|--------:|--------:|---------:|-----:|-------:|------:|------:|----------:|
|    One_String_Literal | 197.4 ns | 1.40 ns | 1.31 ns | 197.2 ns |    1 |      - |     - |     - |         - |
|            One_String | 195.1 ns | 3.70 ns | 3.80 ns | 194.4 ns |    1 |      - |     - |     - |         - |
|    One_Object_Literal | 594.0 ns | 6.15 ns | 5.45 ns | 593.7 ns |    2 | 0.0343 |     - |     - |     144 B |
| One_Object_Dictionary | 739.0 ns | 3.80 ns | 3.55 ns | 738.9 ns |    3 | 0.0420 |     - |     - |     176 B |