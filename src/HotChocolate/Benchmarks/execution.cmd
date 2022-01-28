@echo off

pushd "%~dp0"

if exist BenchmarkDotNet.Artifacts (
	rmdir /s /q BenchmarkDotNet.Artifacts
)
if exist src\Benchmarks.Execution\bin (
	rmdir /s /q src\Benchmarks.Execution\bin
)
if exist src\Benchmarks.Execution\obj (
	rmdir /s /q src\Benchmarks.Execution\obj
)

dotnet run --project src\Benchmarks.Execution\HotChocolate.Benchmarks.Execution.csproj -c release --filter "HotChocolate.Benchmarks.*"

if exist src\Benchmarks.Execution\bin (
	rmdir /s /q src\Benchmarks.Execution\bin
)
if exist src\Benchmarks.Execution\obj (
	rmdir /s /q src\Benchmarks.Execution\obj
)

popd
