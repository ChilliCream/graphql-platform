#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
src=$BASEDIR/src
test=$BASEDIR/test

dotnet format $src/Data
dotnet format $src/Types
dotnet format $test/Data.MongoDb.Filters.Tests
dotnet format $test/Data.MongoDb.Projections.Tests
dotnet format $test/Data.MongoDb.Paging.Tests
dotnet format $test/Data.MongoDb.Sorting.Tests
dotnet format $test/Types.MongoDb
