```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape          | Mean     | Error     | StdDev    | Allocated |
|------------- |--------------- |---------:|----------:|----------:|----------:|
| **ExecuteAsync** | **async-alias2**   | **5.629 μs** | **1.2995 μs** | **1.0146 μs** |   **4.43 KB** |
| **ExecuteAsync** | **async-custom**   | **4.195 μs** | **0.8494 μs** | **0.6631 μs** |   **3.94 KB** |
| **ExecuteAsync** | **async-ordinary** | **4.101 μs** | **0.9044 μs** | **0.7061 μs** |   **3.93 KB** |
| **ExecuteAsync** | **ordinary**       | **1.783 μs** | **0.2757 μs** | **0.2152 μs** |   **3.39 KB** |
