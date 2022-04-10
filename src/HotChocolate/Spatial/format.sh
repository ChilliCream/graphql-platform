#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
src=$BASEDIR/src
test=$BASEDIR/test

dotnet format $src/Data
dotnet format $src/Types
dotnet format $test/Data.Tests
dotnet format $test/Data.Filters.SqlServer.Tests
dotnet format $test/Data.Projections.SqlServer.Tests
dotnet format $test/Types.Tests
