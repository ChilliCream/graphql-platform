```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape         | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------------- |-------------- |---------:|---------:|---------:|-------:|----------:|
| **ExecuteAsync** | **async-batch9**  | **29.83 μs** | **0.983 μs** | **0.767 μs** | **6.0000** |  **34.05 KB** |
| **ExecuteAsync** | **async-single9** | **23.53 μs** | **1.504 μs** | **1.174 μs** | **4.0000** |   **29.7 KB** |
| **ExecuteAsync** | **ordinary**      | **14.13 μs** | **1.232 μs** | **0.962 μs** | **3.0000** |   **25.1 KB** |
