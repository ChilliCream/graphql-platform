#!/usr/bin/env bash

dotnet format src/Core
dotnet format src/Persistence.SQLite
dotnet format src/Razor
dotnet format src/Resources
dotnet format src/Transport.Http
dotnet format src/Transport.InMemory
dotnet format Transport.WebSockets
dotnet format test/Core.Tests
dotnet format test/Persistence.SQLite.Tests
dotnet format test/Transport.Http.Tests
dotnet format test/Transport.InMemory.Tests
dotnet format test/Transport.WebSocket.Tests
