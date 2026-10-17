```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape            | Mean      | Error     | StdDev    | Gen0   | Allocated |
|------------- |----------------- |----------:|----------:|----------:|-------:|----------:|
| **ExecuteAsync** | **alias-sequence10** | **10.968 μs** | **0.9590 μs** | **0.7487 μs** | **2.0000** |  **13.91 KB** |
| **ExecuteAsync** | **alias2**           |  **2.758 μs** | **0.0939 μs** | **0.0733 μs** |      **-** |   **3.72 KB** |
| **ExecuteAsync** | **ordinary**         |  **1.514 μs** | **0.1300 μs** | **0.1015 μs** |      **-** |   **3.29 KB** |
