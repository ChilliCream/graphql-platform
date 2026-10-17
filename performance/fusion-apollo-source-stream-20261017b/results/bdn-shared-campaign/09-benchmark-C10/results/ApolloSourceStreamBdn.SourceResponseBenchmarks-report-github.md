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
| **ReadAsync** | **known128**     |   **783.2 ns** | **333.65 ns** | **260.49 ns** |   **1.16 KB** |
| **ReadAsync** | **unknown32768** | **3,023.7 ns** | **105.20 ns** |  **82.13 ns** |   **1.11 KB** |
| **ReadAsync** | **unknown4096**  |   **928.3 ns** |  **85.67 ns** |  **66.89 ns** |   **1.11 KB** |
