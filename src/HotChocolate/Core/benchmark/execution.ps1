rm -Force BenchmarkDotNet.Artifacts
rm -Force Execution.Benchmarks/bin
rm -Force Execution.Benchmarks/obj

dotnet run --project Execution.Benchmarks/ -c release

rm -Force Execution.Benchmarks/bin
rm -Force Execution.Benchmarks/obj
