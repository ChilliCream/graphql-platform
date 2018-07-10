``` ini

BenchmarkDotNet=v0.10.13, OS=macOS 10.13.5 (17F77) [Darwin 17.6.0]
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical cores and 4 physical cores
.NET Core SDK=2.1.301
  [Host] : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|             Method | Mean | Error | Rank | Allocated |
|------------------- |-----:|------:|-----:|----------:|
|   KitchenSinkQuery |   NA |    NA |    ? |       N/A |
| IntrospectionQuery |   NA |    NA |    ? |       N/A |
|        SimpleQuery |   NA |    NA |    ? |       N/A |
|  KitchenSinkSchema |   NA |    NA |    ? |       N/A |
|       SimpleSchema |   NA |    NA |    ? |       N/A |

Benchmarks with issues:
  LexerBenchmarks.KitchenSinkQuery: Core(Runtime=Core)
  LexerBenchmarks.IntrospectionQuery: Core(Runtime=Core)
  LexerBenchmarks.SimpleQuery: Core(Runtime=Core)
  LexerBenchmarks.KitchenSinkSchema: Core(Runtime=Core)
  LexerBenchmarks.SimpleSchema: Core(Runtime=Core)
