``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.6 (19G2021) [Darwin 19.6.0]
Intel Core i9-9980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=5.0.100-rc.1.20452.10
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.45114, CoreFX 5.0.20.45114), X64 RyuJIT


```
|                                     Method |      Mean |     Error |    StdDev |    Median | Rank |     Gen 0 |     Gen 1 | Gen 2 |   Allocated |
|------------------------------------------- |----------:|----------:|----------:|----------:|-----:|----------:|----------:|------:|------------:|
|                        SchemaIntrospection |  1.101 ms | 0.0213 ms | 0.0228 ms |  1.102 ms |    1 |   58.5938 |    1.9531 |     - |   492.67 KB |
|    SchemaIntrospectionFiveParallelRequests |  3.207 ms | 0.0611 ms | 0.0628 ms |  3.195 ms |    3 |  320.3125 |  105.4688 |     - |  2611.29 KB |
|                                    GetHero |  2.748 ms | 0.0541 ms | 0.0722 ms |  2.781 ms |    2 |         - |         - |     - |     7.24 KB |
|                GetHeroFiveParallelRequests |  2.753 ms | 0.0630 ms | 0.1857 ms |  2.743 ms |    2 |    3.9063 |         - |     - |    36.33 KB |
|                         GetHeroWithFriends | 10.021 ms | 0.1988 ms | 0.4950 ms | 10.165 ms |    6 |         - |         - |     - |    83.37 KB |
|     GetHeroWithFriendsFiveParallelRequests |  9.248 ms | 0.1302 ms | 0.3018 ms |  9.190 ms |    4 |   46.8750 |   15.6250 |     - |   416.94 KB |
|                     GetTwoHerosWithFriends |  9.632 ms | 0.2851 ms | 0.8405 ms |  9.697 ms |    5 |   15.6250 |         - |     - |    180.3 KB |
| GetTwoHerosWithFriendsFiveParallelRequests | 11.148 ms | 0.2508 ms | 0.7396 ms | 11.037 ms |    7 |   93.7500 |   46.8750 |     - |   878.87 KB |
|                                 LargeQuery | 21.020 ms | 0.4192 ms | 0.8175 ms | 21.110 ms |    8 |  406.2500 |  187.5000 |     - |  3470.39 KB |
|             LargeQueryFiveParallelRequests | 50.435 ms | 0.9377 ms | 0.9210 ms | 50.518 ms |    9 | 2181.8182 | 1090.9091 |     - | 17839.82 KB |
