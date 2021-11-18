#!/usr/bin/env bash

dotnet format src/AspNetCore
dotnet format test/AspNetCore.Tests
dotnet format src/AspNetCore.Authorization
dotnet format test/AspNetCore.Authorization.Tests