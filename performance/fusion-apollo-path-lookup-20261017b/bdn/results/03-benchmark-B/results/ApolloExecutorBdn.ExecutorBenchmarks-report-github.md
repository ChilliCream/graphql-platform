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
| **ExecuteAsync** | **batch9**          | **34.97 μs** | **4.225 μs** | **3.298 μs** |
| **ExecuteAsync** | **duplicate96-to1** | **58.08 μs** | **4.912 μs** | **3.835 μs** |
| **ExecuteAsync** | **ordinary**        | **11.89 μs** | **0.229 μs** | **0.178 μs** |
