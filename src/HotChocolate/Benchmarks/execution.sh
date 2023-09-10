#!/bin/sh

BASEDIR=$(dirname "$0")
src=$BASEDIR/src

rm -rf BenchmarkDotNet.Artifacts
rm -rf $src/Benchmarks.Execution/bin
rm -rf $src/Benchmarks.Execution/obj

dotnet run --project $src/Benchmarks.Execution/ -c release --filter HotChocolate.Benchmarks*

rm -rf $src/Benchmarks.Execution/bin
rm -rf $src/Benchmarks.Execution/obj
