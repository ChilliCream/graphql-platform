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
| **ExecuteAsync** | **async-alias2**   | **5.318 μs** | **0.4201 μs** | **0.3280 μs** |   **4.43 KB** |
| **ExecuteAsync** | **async-custom**   | **3.895 μs** | **0.8911 μs** | **0.6957 μs** |   **3.93 KB** |
| **ExecuteAsync** | **async-ordinary** | **3.859 μs** | **0.2341 μs** | **0.1828 μs** |   **3.94 KB** |
| **ExecuteAsync** | **ordinary**       | **1.518 μs** | **0.0831 μs** | **0.0649 μs** |   **3.39 KB** |
