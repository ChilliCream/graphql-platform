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
| **ExecuteAsync** | **async-batch9**  | **29.77 μs** | **1.164 μs** | **0.909 μs** | **6.0000** |      **-** |      **-** |  **33.99 KB** |
| **ExecuteAsync** | **async-single9** | **25.08 μs** | **2.547 μs** | **1.989 μs** | **3.0000** |      **-** |      **-** |  **29.64 KB** |
| **ExecuteAsync** | **ordinary**      | **12.16 μs** | **0.609 μs** | **0.476 μs** | **3.0000** | **1.0000** | **1.0000** | **466.78 KB** |
