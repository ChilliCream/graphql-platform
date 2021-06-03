``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 11.2.3 (20D91) [Darwin 20.3.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=6.0.100-preview.2.21155.3
  [Host]     : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  DefaultJob : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                                     Method |      Mean |     Error |    StdDev |    Median | Rank |     Gen 0 |    Gen 1 |    Gen 2 |   Allocated |
|------------------------------------------- |----------:|----------:|----------:|----------:|-----:|----------:|---------:|---------:|------------:|
|                        SchemaIntrospection |  1.002 ms | 0.0199 ms | 0.0310 ms |  1.010 ms |    1 |   25.3906 |        - |        - |   274.76 KB |
|    SchemaIntrospectionFiveParallelRequests |  5.095 ms | 0.0847 ms | 0.0792 ms |  5.084 ms |    4 |  132.8125 |        - |        - |  1373.79 KB |
|                                    GetHero |  2.308 ms | 0.0449 ms | 0.0481 ms |  2.291 ms |    2 |         - |        - |        - |     8.33 KB |
|                GetHeroFiveParallelRequests |  2.412 ms | 0.0482 ms | 0.0660 ms |  2.396 ms |    3 |    3.9063 |        - |        - |    41.79 KB |
|                         GetHeroWithFriends |  7.976 ms | 0.1332 ms | 0.1180 ms |  7.985 ms |    5 |         - |        - |        - |       88 KB |
|     GetHeroWithFriendsFiveParallelRequests |  8.493 ms | 0.1669 ms | 0.3334 ms |  8.500 ms |    6 |   31.2500 |        - |        - |   412.34 KB |
|                     GetTwoHerosWithFriends |  8.203 ms | 0.1614 ms | 0.3110 ms |  8.337 ms |    5 |   15.6250 |        - |        - |      175 KB |
| GetTwoHerosWithFriendsFiveParallelRequests |  9.610 ms | 0.1888 ms | 0.2174 ms |  9.675 ms |    7 |   78.1250 |  31.2500 |        - |   929.42 KB |
|                                 LargeQuery | 17.607 ms | 0.1932 ms | 0.1713 ms | 17.573 ms |    8 |  312.5000 | 156.2500 |  31.2500 |  3318.11 KB |
|             LargeQueryFiveParallelRequests | 39.444 ms | 0.7677 ms | 0.7181 ms | 39.390 ms |    9 | 1538.4615 | 769.2308 | 153.8462 | 16142.79 KB |
