``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.7 (19H2) [Darwin 19.6.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT


```
|                                     Method |      Mean |     Error |    StdDev |    Median | Rank |     Gen 0 |    Gen 1 |   Gen 2 |  Allocated |
|------------------------------------------- |----------:|----------:|----------:|----------:|-----:|----------:|---------:|--------:|-----------:|
|                        SchemaIntrospection |  1.161 ms | 0.0118 ms | 0.0099 ms |  1.161 ms |    1 |   46.8750 |   1.9531 |       - |  491.12 KB |
|    SchemaIntrospectionFiveParallelRequests |  3.231 ms | 0.0444 ms | 0.0394 ms |  3.220 ms |    4 |  253.9063 |  82.0313 |       - | 2603.97 KB |
|                                    GetHero |  2.287 ms | 0.0399 ms | 0.0392 ms |  2.281 ms |    2 |         - |        - |       - |    7.82 KB |
|                GetHeroFiveParallelRequests |  2.349 ms | 0.0234 ms | 0.0219 ms |  2.353 ms |    3 |         - |        - |       - |   39.27 KB |
|                         GetHeroWithFriends |  8.110 ms | 0.1616 ms | 0.4396 ms |  8.026 ms |    5 |         - |        - |       - |   84.65 KB |
|     GetHeroWithFriendsFiveParallelRequests |  8.439 ms | 0.0702 ms | 0.0622 ms |  8.444 ms |    6 |   31.2500 |  15.6250 |       - |  428.75 KB |
|                     GetTwoHerosWithFriends |  8.017 ms | 0.0826 ms | 0.0645 ms |  8.027 ms |    5 |   15.6250 |        - |       - |  173.61 KB |
| GetTwoHerosWithFriendsFiveParallelRequests |  9.555 ms | 0.1395 ms | 0.1305 ms |  9.564 ms |    7 |   78.1250 |  31.2500 |       - |   869.9 KB |
|                                 LargeQuery | 18.918 ms | 0.2521 ms | 0.2235 ms | 18.842 ms |    8 |  312.5000 | 156.2500 | 31.2500 | 3401.36 KB |
|             LargeQueryFiveParallelRequests | 41.495 ms | 0.7966 ms | 2.0275 ms | 40.848 ms |    9 | 1636.3636 | 818.1818 | 90.9091 | 17145.3 KB |
