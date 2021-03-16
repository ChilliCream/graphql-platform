``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 11.2.3 (20D91) [Darwin 20.3.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.103
  [Host]     : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  DefaultJob : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                                     Method |      Mean |     Error |    StdDev |    Median | Rank |     Gen 0 |    Gen 1 |    Gen 2 |   Allocated |
|------------------------------------------- |----------:|----------:|----------:|----------:|-----:|----------:|---------:|---------:|------------:|
|                        SchemaIntrospection |  1.019 ms | 0.0106 ms | 0.0094 ms |  1.017 ms |    1 |   25.3906 |        - |        - |   277.77 KB |
|    SchemaIntrospectionFiveParallelRequests |  4.872 ms | 0.0636 ms | 0.0595 ms |  4.858 ms |    4 |  132.8125 |        - |        - |  1388.83 KB |
|                                    GetHero |  2.351 ms | 0.0462 ms | 0.0821 ms |  2.370 ms |    2 |         - |        - |        - |     8.18 KB |
|                GetHeroFiveParallelRequests |  2.545 ms | 0.0516 ms | 0.1522 ms |  2.525 ms |    3 |    3.9063 |        - |        - |    41.04 KB |
|                         GetHeroWithFriends |  7.600 ms | 0.0831 ms | 0.0924 ms |  7.593 ms |    5 |         - |        - |        - |    82.35 KB |
|     GetHeroWithFriendsFiveParallelRequests |  8.239 ms | 0.0953 ms | 0.0796 ms |  8.264 ms |    6 |   31.2500 |  15.6250 |        - |    438.4 KB |
|                     GetTwoHerosWithFriends |  8.192 ms | 0.1781 ms | 0.5251 ms |  7.965 ms |    6 |   15.6250 |        - |        - |   178.88 KB |
| GetTwoHerosWithFriendsFiveParallelRequests |  9.350 ms | 0.1801 ms | 0.4139 ms |  9.224 ms |    7 |   78.1250 |  31.2500 |        - |   911.82 KB |
|                                 LargeQuery | 17.720 ms | 0.3495 ms | 0.6303 ms | 17.664 ms |    8 |  312.5000 | 156.2500 |  31.2500 |  3299.39 KB |
|             LargeQueryFiveParallelRequests | 40.000 ms | 0.7795 ms | 0.9573 ms | 39.892 ms |    9 | 1538.4615 | 692.3077 | 153.8462 | 16156.59 KB |
