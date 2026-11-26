# Optional host commands

The example hosts the `catalog` and `reviews` GraphQL schemas and an in-memory Mocha transport named `orders`. Run these commands from this directory.

```sh
dotnet run -- --help
dotnet run -- schema list
dotnet run -- schema export --schema-name reviews --output reviews.graphqls
dotnet run -- mocha transports
```

Starting the app without arguments serves both schemas at `/catalog` and `/reviews`.

## Package references

An application can reference either or both of `HotChocolate.AspNetCore.CommandLine` and `Mocha.CommandLine` and call `app.RunWithCommands(args)` or `app.RunWithCommandsAsync(args)`. Both packages depend on `ChilliCream.Hosting.CommandLine`, which brings in `System.CommandLine` and a C# source generator. With only the main product packages, use `app.Run()` or `app.RunAsync()`.

A CLI package exports a public `IHostCommandModule` implementation with an assembly attribute. The Mocha package declares its module this way.

```csharp
[assembly: HostCommandModule<Mocha.CommandLine.MochaCommands>]
```

The generator emits the host extensions into the calling project and calls each distinct module once when building the command tree. A module receives the invocation's scoped service provider. The runner also includes application contributors registered with `AddHostCommandContributor<T>()`.

For project references from outside this repository, import [HostCommandProjectReferences.targets](../../HostCommandProjectReferences.targets) in your `Directory.Build.props`. NuGet packages and projects under `src/` load the generator automatically.

## Verify the NuGet packages

```sh
python3 verify-packages.py
```

The script packs the example's project dependencies into a temporary feed and builds four .NET 10 applications using NuGet references. It checks command availability for each package combination, both named-schema exports, the configured Mocha transport, invalid commands, and HTTP startup with both CLI packages and with neither. The three CLI applications compile the same `Program.cs`.
