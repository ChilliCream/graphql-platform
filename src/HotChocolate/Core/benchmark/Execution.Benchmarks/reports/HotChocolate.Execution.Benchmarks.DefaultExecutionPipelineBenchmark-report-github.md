``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 11.3 (20E232) [Darwin 20.4.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=6.0.100-preview.3.21202.5
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


```

V11.3
|                                     Method |        Mean |     Error |    StdDev |      Median | Rank |     Gen 0 |    Gen 1 | Gen 2 |   Allocated |
|------------------------------------------- |------------:|----------:|----------:|------------:|-----:|----------:|---------:|------:|------------:|
|                        SchemaIntrospection |    923.7 μs |   7.67 μs |   6.80 μs |    922.4 μs |    1 |   26.3672 |   0.9766 |     - |   275.49 KB |
|    SchemaIntrospectionFiveParallelRequests |  4,809.9 μs |  62.27 μs |  55.20 μs |  4,839.8 μs |    4 |  132.8125 |   7.8125 |     - |  1377.43 KB |
|                                    GetHero |  2,673.3 μs |  22.34 μs |  20.90 μs |  2,672.3 μs |    2 |         - |        - |     - |     9.47 KB |
|                GetHeroFiveParallelRequests |  2,753.5 μs |  38.44 μs |  35.96 μs |  2,748.4 μs |    3 |    3.9063 |        - |     - |    47.68 KB |
|                         GetHeroWithFriends |  9,190.1 μs | 173.89 μs | 178.57 μs |  9,222.3 μs |    5 |         - |        - |     - |    86.66 KB |
|     GetHeroWithFriendsFiveParallelRequests |  9,471.5 μs | 187.96 μs | 443.04 μs |  9,423.1 μs |    5 |   31.2500 |  15.6250 |     - |   421.75 KB |
|                     GetTwoHerosWithFriends |  9,543.8 μs | 187.03 μs | 355.84 μs |  9,682.4 μs |    5 |   15.6250 |        - |     - |   185.38 KB |
| GetTwoHerosWithFriendsFiveParallelRequests | 11,363.6 μs | 173.90 μs | 154.16 μs | 11,400.1 μs |    6 |   78.1250 |  31.2500 |     - |   907.96 KB |
|                                 LargeQuery | 19,371.8 μs | 321.77 μs | 300.98 μs | 19,322.2 μs |    7 |  312.5000 | 156.2500 |     - |  3244.58 KB |
|             LargeQueryFiveParallelRequests | 38,117.5 μs | 753.50 μs | 925.37 μs | 38,035.6 μs |    8 | 1571.4286 | 785.7143 |     - | 16394.95 KB |

V12
|                                      Method |        Mean |     Error |    StdDev |      Median | Rank |     Gen 0 |    Gen 1 | Gen 2 |   Allocated |
|-------------------------------------------- |------------:|----------:|----------:|------------:|-----:|----------:|---------:|------:|------------:|
|                         SchemaIntrospection |    370.0 μs |   1.32 μs |   1.10 μs |    370.0 μs |    1 |   14.6484 |   0.4883 |     - |      151 KB |
|     SchemaIntrospectionFiveParallelRequests |  1,301.1 μs |  25.85 μs |  40.25 μs |  1,312.3 μs |    2 |   83.9844 |  31.2500 |     - |   854.18 KB |
|                                     GetHero |  2,428.6 μs |  28.17 μs |  26.35 μs |  2,432.4 μs |    3 |         - |        - |     - |      7.4 KB |
|                 GetHeroFiveParallelRequests |  2,593.8 μs |  16.14 μs |  15.09 μs |  2,588.6 μs |    4 |         - |        - |     - |    37.14 KB |
|                          GetHeroWithFriends |  8,947.1 μs | 176.88 μs | 230.00 μs |  8,988.2 μs |    5 |         - |        - |     - |    80.66 KB |
|      GetHeroWithFriendsFiveParallelRequests |  9,805.3 μs | 191.62 μs | 212.99 μs |  9,890.8 μs |    7 |   31.2500 |  15.6250 |     - |   434.58 KB |
|                      GetTwoHerosWithFriends |  9,275.2 μs | 181.39 μs | 169.68 μs |  9,336.7 μs |    6 |   15.6250 |        - |     - |   171.16 KB |
| GetTwoHeroesWithFriendsFiveParallelRequests | 10,794.1 μs | 202.93 μs | 199.30 μs | 10,852.7 μs |    8 |   78.1250 |  31.2500 |     - |   891.41 KB |
|                                  LargeQuery | 16,435.2 μs | 309.37 μs | 274.25 μs | 16,501.5 μs |    9 |  281.2500 | 125.0000 |     - |  2917.47 KB |
|              LargeQueryFiveParallelRequests | 37,167.1 μs | 702.93 μs | 809.50 μs | 37,067.3 μs |   10 | 1461.5385 | 692.3077 |     - | 14809.84 KB |

