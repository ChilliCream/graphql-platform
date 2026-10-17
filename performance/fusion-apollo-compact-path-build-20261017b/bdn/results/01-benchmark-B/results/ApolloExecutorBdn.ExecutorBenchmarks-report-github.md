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
| **ExecuteAsync** | **batch9**          | **35.92 μs** | **1.890 μs** | **1.476 μs** | **6.0000** |  **32.75 KB** |
| **ExecuteAsync** | **duplicate96-to1** | **56.13 μs** | **1.044 μs** | **0.815 μs** | **5.0000** |  **28.67 KB** |
| **ExecuteAsync** | **ordinary**        | **11.83 μs** | **0.197 μs** | **0.154 μs** | **3.0000** |  **25.08 KB** |
