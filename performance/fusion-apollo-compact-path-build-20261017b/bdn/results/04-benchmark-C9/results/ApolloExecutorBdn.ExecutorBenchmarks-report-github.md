```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape           | Mean     | Error    | StdDev   | Gen0   | Gen1   | Gen2   | Allocated |
|------------- |---------------- |---------:|---------:|---------:|-------:|-------:|-------:|----------:|
| **ExecuteAsync** | **batch9**          | **33.70 μs** | **4.440 μs** | **3.467 μs** | **6.0000** |      **-** |      **-** |  **32.74 KB** |
| **ExecuteAsync** | **duplicate96-to1** | **55.65 μs** | **1.341 μs** | **1.047 μs** | **3.0000** | **1.0000** | **1.0000** |  **28.68 KB** |
| **ExecuteAsync** | **ordinary**        | **11.68 μs** | **0.275 μs** | **0.215 μs** | **3.0000** |      **-** |      **-** |  **25.08 KB** |
