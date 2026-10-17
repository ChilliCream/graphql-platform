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
| **ReadAsync** | **known128**     |   **752.0 ns** | **270.59 ns** | **211.26 ns** |   **1.16 KB** |
| **ReadAsync** | **unknown32768** | **3,258.4 ns** |  **99.90 ns** |  **78.00 ns** |   **1.39 KB** |
| **ReadAsync** | **unknown4096**  |   **999.3 ns** |  **85.48 ns** |  **66.73 ns** |   **1.25 KB** |
