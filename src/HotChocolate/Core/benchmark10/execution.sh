#!/bin/sh

rm -rf BenchmarkDotNet.Artifacts
rm -rf Execution.Benchmarks/bin
rm -rf Execution.Benchmarks/obj

dotnet run --project Execution.Benchmarks/ -c release

rm -rf Execution.Benchmarks/bin
rm -rf Execution.Benchmarks/obj
