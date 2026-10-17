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
| **ExecuteAsync** | **alias-sequence10** | **10.559 μs** | **0.3283 μs** | **0.2563 μs** | **2.0000** |  **14.28 KB** |
| **ExecuteAsync** | **alias2**           |  **2.753 μs** | **0.0795 μs** | **0.0621 μs** |      **-** |   **3.81 KB** |
| **ExecuteAsync** | **ordinary**         |  **1.494 μs** | **0.1213 μs** | **0.0947 μs** |      **-** |   **3.39 KB** |
