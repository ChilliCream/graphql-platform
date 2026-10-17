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
| **ExecuteAsync** | **async-alias2**   | **5.295 μs** | **0.1399 μs** | **0.1092 μs** |   **4.29 KB** |
| **ExecuteAsync** | **async-custom**   | **3.848 μs** | **0.0995 μs** | **0.0777 μs** |   **3.94 KB** |
| **ExecuteAsync** | **async-ordinary** | **4.315 μs** | **1.1800 μs** | **0.9212 μs** |    **3.8 KB** |
| **ExecuteAsync** | **ordinary**       | **1.633 μs** | **0.1091 μs** | **0.0852 μs** |   **3.33 KB** |
