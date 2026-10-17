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
| **ExecuteAsync** | **batch9**          | **34.90 μs** | **2.712 μs** | **2.117 μs** | **6.0000** |      **-** |      **-** |  **32.75 KB** |
| **ExecuteAsync** | **duplicate96-to1** | **54.89 μs** | **2.111 μs** | **1.648 μs** | **3.0000** | **1.0000** | **1.0000** |  **28.68 KB** |
| **ExecuteAsync** | **ordinary**        | **11.79 μs** | **0.450 μs** | **0.352 μs** | **3.0000** |      **-** |      **-** |  **25.08 KB** |
