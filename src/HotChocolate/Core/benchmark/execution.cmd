@echo off

pushd "%~dp0"

if exist BenchmarkDotNet.Artifacts (
	rmdir /s /q BenchmarkDotNet.Artifacts
)
if exist Execution.Benchmarks\bin (
	rmdir /s /q Execution.Benchmarks\bin
)
if exist Execution.Benchmarks\obj (
	rmdir /s /q Execution.Benchmarks\obj
)

dotnet run --project Execution.Benchmarks\HotChocolate.Execution.Benchmarks.csproj -c release

if exist Execution.Benchmarks\bin (
	rmdir /s /q Execution.Benchmarks\bin
)
if exist Execution.Benchmarks\obj (
	rmdir /s /q Execution.Benchmarks\obj
)

popd
