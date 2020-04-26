#!/bin/sh

rm -rf BenchmarkDotNet.Artifacts
rm -rf Language.Visitors.Benchmarks/bin
rm -rf Language.Visitors.Benchmarks/obj

dotnet run --project  Language.Visitors.Benchmarks/ -c release

rm -rf Language.Visitors.Benchmarks/bin
rm -rf Language.Visitors.Benchmarks/obj
