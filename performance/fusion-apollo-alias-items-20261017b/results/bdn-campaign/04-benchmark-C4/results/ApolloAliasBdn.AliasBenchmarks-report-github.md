```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape            | Mean      | Error     | StdDev    | Median    | Gen0   | Allocated |
|------------- |----------------- |----------:|----------:|----------:|----------:|-------:|----------:|
| **ExecuteAsync** | **alias-sequence10** | **10.739 μs** | **0.4464 μs** | **0.3485 μs** | **10.647 μs** | **4.0000** |  **13.52 KB** |
| **ExecuteAsync** | **alias2**           |  **2.772 μs** | **0.0958 μs** | **0.0748 μs** |  **2.806 μs** |      **-** |   **3.65 KB** |
| **ExecuteAsync** | **ordinary**         |  **2.473 μs** | **2.3377 μs** | **1.8251 μs** |  **1.751 μs** |      **-** |   **3.39 KB** |
