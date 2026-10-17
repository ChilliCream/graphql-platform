```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape           | Mean     | Error    | StdDev   |
|------------- |---------------- |---------:|---------:|---------:|
| **ExecuteAsync** | **batch9**          | **34.86 μs** | **3.337 μs** | **2.605 μs** |
| **ExecuteAsync** | **duplicate96-to1** | **55.74 μs** | **2.548 μs** | **1.990 μs** |
| **ExecuteAsync** | **ordinary**        | **11.88 μs** | **0.215 μs** | **0.168 μs** |
