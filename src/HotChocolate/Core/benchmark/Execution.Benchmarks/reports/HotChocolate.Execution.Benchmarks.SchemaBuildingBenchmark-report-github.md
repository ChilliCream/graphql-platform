``` ini

BenchmarkDotNet=v0.12.1, OS=macOS 11.2.3 (20D91) [Darwin 20.3.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.103
  [Host]     : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  DefaultJob : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|             Method |      Mean |    Error |   StdDev |    Median | Rank |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|------------------- |----------:|---------:|---------:|----------:|-----:|----------:|----------:|----------:|----------:|
|           StarWars |  32.16 ms | 0.640 ms | 1.404 ms |  32.06 ms |    1 |         - |         - |         - |   3.96 MB |
| GitHub_SchemaFirst | 154.26 ms | 3.032 ms | 2.836 ms | 153.51 ms |    3 | 4000.0000 | 2000.0000 | 1000.0000 |  38.96 MB |
|          CodeFirst |  57.90 ms | 1.140 ms | 2.170 ms |  57.84 ms |    2 | 1000.0000 |         - |         - |  12.61 MB |
