@echo off

pushd "%~dp0"

if exist BenchmarkDotNet.Artifacts (
	rmdir /s /q BenchmarkDotNet.Artifacts
)
if exist Validation.Benchmarks\bin (
	rmdir /s /q Validation.Benchmarks\bin
)
if exist Validation.Benchmarks\obj (
	rmdir /s /q Validation.Benchmarks\obj
)

dotnet run --project Validation.Benchmarks\HotChocolate.Validation.Benchmarks.csproj -c release

if exist Validation.Benchmarks\bin (
	rmdir /s /q Validation.Benchmarks\bin
)
if exist Validation.Benchmarks\obj (
	rmdir /s /q Validation.Benchmarks\obj
)

popd
