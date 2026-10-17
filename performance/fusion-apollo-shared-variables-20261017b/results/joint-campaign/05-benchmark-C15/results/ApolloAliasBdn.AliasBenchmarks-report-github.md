```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
-
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Job=DefaultGC-1000AwaitedOperations  OutlierMode=DontRemove  Force=False  
Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=12  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method       | Shape          | Mean     | Error     | StdDev    | Allocated |
|------------- |--------------- |---------:|----------:|----------:|----------:|
| **ExecuteAsync** | **async-alias2**   | **5.393 μs** | **1.2534 μs** | **0.9786 μs** |   **4.39 KB** |
| **ExecuteAsync** | **async-custom**   | **3.682 μs** | **1.8255 μs** | **1.4252 μs** |   **3.93 KB** |
| **ExecuteAsync** | **async-ordinary** | **3.441 μs** | **0.7036 μs** | **0.5493 μs** |   **3.94 KB** |
| **ExecuteAsync** | **ordinary**       | **1.604 μs** | **0.1196 μs** | **0.0934 μs** |   **3.38 KB** |
