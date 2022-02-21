#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
src=$BASEDIR/src
test=$BASEDIR/test

dotnet format $src/AspNetCore
dotnet format $test/AspNetCore.Tests
dotnet format $src/AspNetCore.Authorization
dotnet format $test/AspNetCore.Authorization.Tests