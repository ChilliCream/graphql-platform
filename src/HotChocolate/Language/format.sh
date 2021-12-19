#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
src=$BASEDIR/src
test=$BASEDIR/test

dotnet format $src/Language
dotnet format $src/Language.SyntaxTree
dotnet format $src/Language.Utf8
dotnet format $src/Language.Visitors
dotnet format $test/Language.Tests