#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
src=$BASEDIR/src
test=$BASEDIR/test
examples=$BASEDIR/examples/ApolloFederation.OfficialDemo/Services

dotnet format $src/ApolloFederation
dotnet format $test/ApolloFederation.Tests
dotnet format $examples/Accounts
dotnet format $examples/Inventory
dotnet format $examples/Products
dotnet format $examples/Reviews
dotnet format $examples/Services.Reviews
