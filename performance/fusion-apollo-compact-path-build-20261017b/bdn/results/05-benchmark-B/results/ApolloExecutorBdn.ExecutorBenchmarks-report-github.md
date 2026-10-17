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
| **ExecuteAsync** | **batch9**          | **34.97 μs** | **3.963 μs** | **3.094 μs** | **6.0000** |  **32.77 KB** |
| **ExecuteAsync** | **duplicate96-to1** | **59.69 μs** | **5.403 μs** | **4.219 μs** | **5.0000** |   **28.7 KB** |
| **ExecuteAsync** | **ordinary**        | **12.05 μs** | **0.182 μs** | **0.142 μs** | **3.0000** |  **25.11 KB** |
