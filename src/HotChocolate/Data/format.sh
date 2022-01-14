#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
src=$BASEDIR/src
test=$BASEDIR/test

dotnet format $src/Data
dotnet format $src/EntityFramework
dotnet format $test/Data.EntityFramework.Tests
dotnet format $test/Data.Filters.InMemory.Tests
dotnet format $test/Data.Filters.SqlServer.Tests
dotnet format $test/Data.Filters.Tests
dotnet format $test/Data.Projections.SqlServer.Tests
dotnet format $test/Data.Projections.Tests
dotnet format $test/Data.Sorting.InMemory.Tests
dotnet format $test/Data.Sorting.SqlLite.Tests
dotnet format $test/Data.Sorting.Tests
dotnet format $test/Data.Tests