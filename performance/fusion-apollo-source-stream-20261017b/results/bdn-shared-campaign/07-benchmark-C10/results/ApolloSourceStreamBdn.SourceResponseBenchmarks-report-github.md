```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000SourceResponses  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method    | Shape        | Mean       | Error     | StdDev    | Allocated |
|---------- |------------- |-----------:|----------:|----------:|----------:|
| **ReadAsync** | **known128**     |   **744.9 ns** | **284.84 ns** | **222.39 ns** |   **1.16 KB** |
| **ReadAsync** | **unknown32768** | **3,022.0 ns** | **115.27 ns** |  **90.00 ns** |   **1.11 KB** |
| **ReadAsync** | **unknown4096**  |   **935.6 ns** |  **90.20 ns** |  **70.43 ns** |   **1.11 KB** |
