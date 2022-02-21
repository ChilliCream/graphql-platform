#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
src=$BASEDIR/src
test=$BASEDIR/test

dotnet format $src/Diagnostics
dotnet format $test/Diagnostics.Tests