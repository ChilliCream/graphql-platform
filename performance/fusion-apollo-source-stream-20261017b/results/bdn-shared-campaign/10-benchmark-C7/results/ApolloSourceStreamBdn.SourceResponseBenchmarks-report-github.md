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
| **ReadAsync** | **known128**     |   **756.2 ns** | **313.06 ns** | **244.41 ns** |    **1192 B** |
| **ReadAsync** | **unknown32768** | **2,312.4 ns** | **106.01 ns** |  **82.77 ns** |    **1099 B** |
| **ReadAsync** | **unknown4096**  |   **845.4 ns** |  **91.62 ns** |  **71.53 ns** |     **952 B** |
