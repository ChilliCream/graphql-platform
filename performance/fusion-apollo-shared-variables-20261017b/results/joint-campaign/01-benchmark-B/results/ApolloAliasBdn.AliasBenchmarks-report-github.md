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
| **ExecuteAsync** | **async-alias2**   | **5.186 μs** | **1.1444 μs** | **0.8935 μs** |   **4.43 KB** |
| **ExecuteAsync** | **async-custom**   | **4.000 μs** | **0.1675 μs** | **0.1308 μs** |   **3.94 KB** |
| **ExecuteAsync** | **async-ordinary** | **4.594 μs** | **1.2698 μs** | **0.9913 μs** |   **3.93 KB** |
| **ExecuteAsync** | **ordinary**       | **1.591 μs** | **0.1136 μs** | **0.0887 μs** |   **3.38 KB** |
