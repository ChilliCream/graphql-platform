```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape            | Mean      | Error     | StdDev    | Gen0   | Gen1   | Gen2   | Allocated |
|------------- |----------------- |----------:|----------:|----------:|-------:|-------:|-------:|----------:|
| **ExecuteAsync** | **alias-sequence10** | **10.842 μs** | **0.4482 μs** | **0.3499 μs** | **1.0000** | **1.0000** | **1.0000** |  **14.29 KB** |
| **ExecuteAsync** | **alias2**           |  **2.701 μs** | **0.1178 μs** | **0.0920 μs** |      **-** |      **-** |      **-** |   **3.81 KB** |
| **ExecuteAsync** | **ordinary**         |  **1.545 μs** | **0.1119 μs** | **0.0873 μs** |      **-** |      **-** |      **-** |   **3.39 KB** |
