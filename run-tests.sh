dotnet build src
dotnet test src/Runtime.Tests
dotnet test src/Language.Tests --no-build
dotnet test src/Core.Tests --no-build
dotnet test src/AspNetCore.Tests --no-build
