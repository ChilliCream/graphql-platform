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
| **ExecuteAsync** | **async-batch9**  | **29.16 μs** | **0.809 μs** | **0.631 μs** | **6.0000** |      **-** |      **-** |  **33.86 KB** |
| **ExecuteAsync** | **async-single9** | **22.35 μs** | **1.266 μs** | **0.988 μs** | **4.0000** | **1.0000** | **1.0000** | **146.18 KB** |
| **ExecuteAsync** | **ordinary**      | **12.20 μs** | **0.439 μs** | **0.342 μs** | **4.0000** | **1.0000** | **1.0000** | **467.91 KB** |
