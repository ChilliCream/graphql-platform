``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19044
11th Gen Intel Core i7-11700F 2.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=6.0.101
  [Host]     : .NET Core 6.0.1 (CoreCLR 6.0.121.56705, CoreFX 6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET Core 6.0.1 (CoreCLR 6.0.121.56705, CoreFX 6.0.121.56705), X64 RyuJIT


```
|                                      Method |        Mean |       Error |      StdDev |      Median | Rank |     Gen 0 |    Gen 1 | Gen 2 |   Allocated |
|-------------------------------------------- |------------:|------------:|------------:|------------:|-----:|----------:|---------:|------:|------------:|
|                         SchemaIntrospection |    242.3 μs |     0.87 μs |     0.82 μs |    242.1 μs |    1 |   10.2539 |        - |     - |    84.12 KB |
|     SchemaIntrospectionFiveParallelRequests |  1,219.7 μs |     9.00 μs |     8.42 μs |  1,218.8 μs |    2 |   50.7813 |        - |     - |   420.59 KB |
|                                     GetHero | 15,562.3 μs |   248.69 μs |   232.62 μs | 15,702.1 μs |    3 |         - |        - |     - |     5.58 KB |
|                 GetHeroFiveParallelRequests | 15,590.7 μs |   207.16 μs |   193.78 μs | 15,693.9 μs |    3 |         - |        - |     - |    27.68 KB |
|                          GetHeroWithFriends | 46,728.6 μs |   610.80 μs |   541.46 μs | 46,978.4 μs |    4 |         - |        - |     - |    51.14 KB |
|      GetHeroWithFriendsFiveParallelRequests | 46,902.5 μs |   192.30 μs |   150.13 μs | 46,944.4 μs |    4 |         - |        - |     - |   256.59 KB |
|                      GetTwoHerosWithFriends | 46,873.5 μs |   448.68 μs |   397.74 μs | 46,985.8 μs |    4 |         - |        - |     - |   109.06 KB |
| GetTwoHeroesWithFriendsFiveParallelRequests | 47,202.2 μs |   753.44 μs |   704.77 μs | 46,887.3 μs |    4 |         - |        - |     - |   529.34 KB |
|                                  LargeQuery | 64,337.4 μs | 1,276.28 μs | 1,987.02 μs | 64,146.8 μs |    5 |  125.0000 |        - |     - |  1914.96 KB |
|              LargeQueryFiveParallelRequests | 73,617.7 μs | 1,814.20 μs | 5,349.20 μs | 73,850.1 μs |    6 | 1142.8571 | 571.4286 |     - | 10218.22 KB |
