#!/bin/sh

rm -rf BenchmarkDotNet.Artifacts
rm -rf Validation.Benchmarks/bin
rm -rf Validation.Benchmarks/obj

dotnet run --project Validation.Benchmarks/ -c release

rm -rf Validation.Benchmarks/bin
rm -rf Validation.Benchmarks/obj
