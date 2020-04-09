rm -Force BenchmarkDotNet.Artifacts
rm -Force Validation.Benchmarks/bin
rm -Force Validation.Benchmarks/obj

dotnet run --project Validation.Benchmarks/ -c release

rm -Force Validation.Benchmarks/bin
rm -Force Validation.Benchmarks/obj
