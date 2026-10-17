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
| **ExecuteAsync** | **async-alias2**   | **5.942 μs** | **1.1586 μs** | **0.9046 μs** |    **4.4 KB** |
| **ExecuteAsync** | **async-custom**   | **3.401 μs** | **1.0160 μs** | **0.7932 μs** |   **3.94 KB** |
| **ExecuteAsync** | **async-ordinary** | **4.371 μs** | **0.7791 μs** | **0.6082 μs** |   **3.94 KB** |
| **ExecuteAsync** | **ordinary**       | **1.607 μs** | **0.2015 μs** | **0.1573 μs** |   **3.39 KB** |
