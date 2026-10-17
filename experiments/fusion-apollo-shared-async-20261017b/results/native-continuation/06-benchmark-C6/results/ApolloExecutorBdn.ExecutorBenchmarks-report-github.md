```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape         | Mean     | Error    | StdDev   | Gen0   | Gen1   | Gen2   | Allocated |
|------------- |-------------- |---------:|---------:|---------:|-------:|-------:|-------:|----------:|
| **ExecuteAsync** | **async-batch9**  | **29.45 μs** | **0.605 μs** | **0.472 μs** | **6.0000** |      **-** |      **-** |  **33.96 KB** |
| **ExecuteAsync** | **async-single9** | **23.74 μs** | **1.368 μs** | **1.068 μs** | **4.0000** |      **-** |      **-** |  **29.62 KB** |
| **ExecuteAsync** | **ordinary**      | **12.04 μs** | **0.763 μs** | **0.595 μs** | **4.0000** | **1.0000** | **1.0000** |  **141.8 KB** |
