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
| **ExecuteAsync** | **alias-sequence10** | **12.161 μs** | **1.2433 μs** | **0.9707 μs** | **2.0000** | **1.0000** | **1.0000** |  **14.04 KB** |
| **ExecuteAsync** | **alias2**           |  **2.850 μs** | **0.2170 μs** | **0.1694 μs** |      **-** |      **-** |      **-** |   **3.76 KB** |
| **ExecuteAsync** | **ordinary**         |  **1.515 μs** | **0.1280 μs** | **0.0999 μs** |      **-** |      **-** |      **-** |   **3.39 KB** |
