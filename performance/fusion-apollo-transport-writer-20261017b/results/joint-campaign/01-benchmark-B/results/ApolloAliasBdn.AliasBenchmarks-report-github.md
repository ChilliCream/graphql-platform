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
| **ExecuteAsync** | **alias-sequence10** | **10.337 μs** | **0.6054 μs** | **0.4727 μs** | **2.0000** |  **14.29 KB** |
| **ExecuteAsync** | **alias2**           |  **2.628 μs** | **0.1348 μs** | **0.1052 μs** |      **-** |    **3.8 KB** |
| **ExecuteAsync** | **ordinary**         |  **1.492 μs** | **0.1151 μs** | **0.0899 μs** |      **-** |   **3.39 KB** |
