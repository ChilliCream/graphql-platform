``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19044
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=6.0.101
  [Host]     : .NET Core 6.0.1 (CoreCLR 6.0.121.56705, CoreFX 6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET Core 6.0.1 (CoreCLR 6.0.121.56705, CoreFX 6.0.121.56705), X64 RyuJIT


```
|                                      Method |         Mean |       Error |       StdDev |       Median | Rank |     Gen 0 |    Gen 1 | Gen 2 |  Allocated |
|-------------------------------------------- |-------------:|------------:|-------------:|-------------:|-----:|----------:|---------:|------:|-----------:|
|                         SchemaIntrospection |     251.7 μs |     1.11 μs |      0.99 μs |     251.3 μs |    1 |   10.2539 |        - |     - |   84.12 KB |
|     SchemaIntrospectionFiveParallelRequests |   1,219.2 μs |     5.32 μs |      4.71 μs |   1,218.6 μs |    2 |   50.7813 |        - |     - |  420.59 KB |
|        SchemaIntrospectionFiveInSerialBatch |   1,510.7 μs |     7.97 μs |      7.45 μs |   1,509.8 μs |    3 |   74.2188 |   1.9531 |     - |  606.81 KB |
|      SchemaIntrospectionFiveInParallelBatch |   1,513.3 μs |     3.84 μs |      3.60 μs |   1,512.9 μs |    3 |   74.2188 |   1.9531 |     - |  606.81 KB |
|                                     GetHero |  15,594.6 μs |   281.65 μs |    263.46 μs |  15,693.0 μs |    4 |         - |        - |     - |    5.59 KB |
|                 GetHeroFiveParallelRequests |  15,641.5 μs |   159.85 μs |    149.52 μs |  15,545.0 μs |    4 |         - |        - |     - |   27.65 KB |
|                    GetHeroFiveInSerialBatch |  77,848.4 μs |   881.47 μs |    824.53 μs |  78,219.5 μs |    8 |         - |        - |     - |   60.45 KB |
|                  GetHeroFiveInParallelBatch |  78,228.2 μs |   657.85 μs |    615.35 μs |  78,423.3 μs |    8 |         - |        - |     - |   60.47 KB |
|                          GetHeroWithFriends |  46,772.1 μs |   545.68 μs |    483.73 μs |  46,950.2 μs |    5 |         - |        - |     - |   50.09 KB |
|      GetHeroWithFriendsFiveParallelRequests |  46,818.6 μs |   168.90 μs |    131.87 μs |  46,822.8 μs |    5 |         - |        - |     - |  252.49 KB |
|         GetHeroWithFriendsFiveInSerialBatch | 236,324.2 μs | 3,724.09 μs |  3,483.52 μs | 234,203.5 μs |    9 |         - |        - |     - |  409.66 KB |
|       GetHeroWithFriendsFiveInParallelBatch | 234,871.3 μs | 2,519.53 μs |  2,233.50 μs | 233,957.0 μs |    9 |         - |        - |     - |  409.18 KB |
|                      GetTwoHerosWithFriends |  46,688.1 μs |   527.31 μs |    467.44 μs |  46,872.2 μs |    5 |         - |        - |     - |  107.25 KB |
| GetTwoHeroesWithFriendsFiveParallelRequests |  47,517.4 μs |   907.10 μs |    848.50 μs |  47,784.0 μs |    5 |         - |        - |     - |  530.15 KB |
|    GetTwoHeroesWithFriendsFiveInSerialBatch | 235,546.6 μs | 2,977.30 μs |  2,784.97 μs | 233,942.4 μs |    9 |         - |        - |     - |  697.82 KB |
|  GetTwoHeroesWithFriendsFiveInParallelBatch | 235,878.3 μs | 4,249.61 μs |  3,975.09 μs | 233,863.3 μs |    9 |         - |        - |     - |   730.2 KB |
|                                  LargeQuery |  64,505.5 μs | 1,264.76 μs |  2,248.11 μs |  64,188.9 μs |    6 |  125.0000 |        - |     - | 1822.12 KB |
|              LargeQueryFiveParallelRequests |  73,863.7 μs | 1,630.90 μs |  4,808.76 μs |  73,192.9 μs |    7 | 1142.8571 | 571.4286 |     - |  9973.9 KB |
|                 LargeQueryFiveInSerialBatch | 321,679.4 μs | 6,415.92 μs | 12,361.30 μs | 316,285.1 μs |   10 | 1000.0000 |        - |     - | 10904.7 KB |
|               LargeQueryFiveInParallelBatch | 320,374.9 μs | 6,311.97 μs | 14,375.56 μs | 316,233.0 μs |   10 | 1000.0000 |        - |     - | 9690.19 KB |
