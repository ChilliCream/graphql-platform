#!/usr/bin/env bash

dotnet format src/CodeGeneration
dotnet format src/CodeGeneration.CSharp
dotnet format src/CodeGeneration.CSharp.Client
dotnet format src/CodeGeneration.CSharp.Server
dotnet format test/CodeGeneration.CSharp.Server.Tests
#dotnet format test/CodeGeneration.CSharp.Tests
dotnet format test/CodeGeneration.Razor.Tests
dotnet format test/CodeGeneration.Tests