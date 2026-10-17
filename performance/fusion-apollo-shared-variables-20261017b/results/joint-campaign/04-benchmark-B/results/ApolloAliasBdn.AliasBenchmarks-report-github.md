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
| **ExecuteAsync** | **async-alias2**   | **5.136 μs** | **0.3358 μs** | **0.2622 μs** |   **4.43 KB** |
| **ExecuteAsync** | **async-custom**   | **3.730 μs** | **0.1750 μs** | **0.1366 μs** |   **3.93 KB** |
| **ExecuteAsync** | **async-ordinary** | **2.869 μs** | **0.9591 μs** | **0.7488 μs** |   **3.94 KB** |
| **ExecuteAsync** | **ordinary**       | **1.621 μs** | **0.1113 μs** | **0.0869 μs** |   **3.39 KB** |
