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
| **ExecuteAsync** | **async-alias2**   | **5.383 μs** | **0.1773 μs** | **0.1385 μs** |   **4.39 KB** |
| **ExecuteAsync** | **async-custom**   | **3.808 μs** | **0.1149 μs** | **0.0897 μs** |   **3.94 KB** |
| **ExecuteAsync** | **async-ordinary** | **3.812 μs** | **0.1055 μs** | **0.0824 μs** |   **3.93 KB** |
| **ExecuteAsync** | **ordinary**       | **1.680 μs** | **0.2695 μs** | **0.2104 μs** |   **3.38 KB** |
