``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.7 (19H15) [Darwin 19.6.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.100-rc.2.20479.15
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.47505, CoreFX 5.0.20.47505), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.47505, CoreFX 5.0.20.47505), X64 RyuJIT


```
|                                     Method |      Mean |     Error |    StdDev |    Median | Rank |     Gen 0 |    Gen 1 | Gen 2 |   Allocated |
|------------------------------------------- |----------:|----------:|----------:|----------:|-----:|----------:|---------:|------:|------------:|
|                        SchemaIntrospection |  1.038 ms | 0.0088 ms | 0.0082 ms |  1.038 ms |    1 |   48.8281 |   1.9531 |     - |    503.4 KB |
|    SchemaIntrospectionFiveParallelRequests |  2.791 ms | 0.0550 ms | 0.0977 ms |  2.823 ms |    4 |  261.7188 | 101.5625 |     - |  2671.83 KB |
|                                    GetHero |  2.408 ms | 0.0139 ms | 0.0130 ms |  2.409 ms |    2 |         - |        - |     - |     8.17 KB |
|                GetHeroFiveParallelRequests |  2.602 ms | 0.0184 ms | 0.0173 ms |  2.600 ms |    3 |    3.9063 |        - |     - |    41.01 KB |
|                         GetHeroWithFriends |  8.019 ms | 0.0422 ms | 0.0352 ms |  8.021 ms |    5 |         - |        - |     - |     84.4 KB |
|     GetHeroWithFriendsFiveParallelRequests |  8.744 ms | 0.0813 ms | 0.0635 ms |  8.758 ms |    7 |   31.2500 |  15.6250 |     - |   418.41 KB |
|                     GetTwoHerosWithFriends |  8.327 ms | 0.0582 ms | 0.0544 ms |  8.334 ms |    6 |   15.6250 |        - |     - |    176.3 KB |
| GetTwoHerosWithFriendsFiveParallelRequests |  9.537 ms | 0.0725 ms | 0.0678 ms |  9.539 ms |    8 |   78.1250 |  31.2500 |     - |   913.14 KB |
|                                 LargeQuery | 17.247 ms | 0.1668 ms | 0.1479 ms | 17.295 ms |    9 |  343.7500 | 156.2500 |     - |  3692.05 KB |
|             LargeQueryFiveParallelRequests | 39.248 ms | 0.7407 ms | 0.6929 ms | 39.304 ms |   10 | 1692.3077 | 846.1538 |     - | 17823.86 KB |
