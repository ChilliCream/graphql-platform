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
| **ExecuteAsync** | **async-alias2**   | **5.481 μs** | **0.6417 μs** | **0.5010 μs** |    **4.4 KB** |
| **ExecuteAsync** | **async-custom**   | **3.964 μs** | **0.1281 μs** | **0.1000 μs** |   **3.93 KB** |
| **ExecuteAsync** | **async-ordinary** | **4.386 μs** | **0.8498 μs** | **0.6634 μs** |   **3.94 KB** |
| **ExecuteAsync** | **ordinary**       | **1.635 μs** | **0.1421 μs** | **0.1109 μs** |   **3.39 KB** |
