```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape          | Mean     | Error     | StdDev    | Gen0   | Gen1   | Gen2   | Allocated |
|------------- |--------------- |---------:|----------:|----------:|-------:|-------:|-------:|----------:|
| **ExecuteAsync** | **async-alias2**   | **6.047 μs** | **0.6351 μs** | **0.4959 μs** | **1.0000** | **1.0000** | **1.0000** |   **4.29 KB** |
| **ExecuteAsync** | **async-custom**   | **3.654 μs** | **0.6352 μs** | **0.4960 μs** |      **-** |      **-** |      **-** |   **3.94 KB** |
| **ExecuteAsync** | **async-ordinary** | **4.068 μs** | **0.2442 μs** | **0.1907 μs** |      **-** |      **-** |      **-** |   **3.79 KB** |
| **ExecuteAsync** | **ordinary**       | **2.129 μs** | **0.2640 μs** | **0.2061 μs** |      **-** |      **-** |      **-** |   **3.32 KB** |
