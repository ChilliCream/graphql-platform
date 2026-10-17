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
| **ExecuteAsync** | **alias-sequence10** | **11.161 μs** | **0.5918 μs** | **0.4620 μs** | **2.0000** |  **14.28 KB** |
| **ExecuteAsync** | **alias2**           |  **2.701 μs** | **0.1304 μs** | **0.1018 μs** |      **-** |   **3.81 KB** |
| **ExecuteAsync** | **ordinary**         |  **1.508 μs** | **0.0733 μs** | **0.0572 μs** |      **-** |   **3.39 KB** |
