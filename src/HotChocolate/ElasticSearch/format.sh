#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
src=$BASEDIR/src
test=$BASEDIR/test

dotnet format $src/Data.ElasticSearch
dotnet format $src/Data.ElasticSearch.Driver
dotnet format $test/Data.ElasticSearch.Tests
