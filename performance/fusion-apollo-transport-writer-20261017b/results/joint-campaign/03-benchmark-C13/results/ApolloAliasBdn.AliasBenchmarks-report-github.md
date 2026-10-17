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
| **ExecuteAsync** | **alias-sequence10** | **10.444 μs** | **0.3067 μs** | **0.2395 μs** | **1.0000** | **1.0000** | **1.0000** |  **14.04 KB** |
| **ExecuteAsync** | **alias2**           |  **2.724 μs** | **0.0773 μs** | **0.0604 μs** |      **-** |      **-** |      **-** |   **3.75 KB** |
| **ExecuteAsync** | **ordinary**         |  **1.467 μs** | **0.1199 μs** | **0.0936 μs** |      **-** |      **-** |      **-** |   **3.39 KB** |
