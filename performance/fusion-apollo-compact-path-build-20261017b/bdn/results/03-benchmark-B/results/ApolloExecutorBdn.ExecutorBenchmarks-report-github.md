```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape           | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------------- |---------------- |---------:|---------:|---------:|-------:|----------:|
| **ExecuteAsync** | **batch9**          | **34.61 μs** | **5.318 μs** | **4.152 μs** | **6.0000** |  **32.74 KB** |
| **ExecuteAsync** | **duplicate96-to1** | **56.22 μs** | **1.737 μs** | **1.356 μs** | **5.0000** |  **28.66 KB** |
| **ExecuteAsync** | **ordinary**        | **12.20 μs** | **0.436 μs** | **0.341 μs** | **4.0000** |  **25.08 KB** |
