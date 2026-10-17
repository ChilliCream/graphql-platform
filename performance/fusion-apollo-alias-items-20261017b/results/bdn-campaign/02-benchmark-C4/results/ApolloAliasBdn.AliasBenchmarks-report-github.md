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
| **ExecuteAsync** | **alias-sequence10** | **10.567 μs** | **0.3874 μs** | **0.3024 μs** | **3.0000** | **1.0000** | **1.0000** | **214.77 KB** |
| **ExecuteAsync** | **alias2**           |  **2.777 μs** | **0.1039 μs** | **0.0811 μs** |      **-** |      **-** |      **-** |   **3.65 KB** |
| **ExecuteAsync** | **ordinary**         |  **1.523 μs** | **0.1005 μs** | **0.0785 μs** |      **-** |      **-** |      **-** |   **3.39 KB** |
