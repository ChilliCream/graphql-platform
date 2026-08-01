using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Aspire;

public sealed class GraphQLSchemaExportCommandTests
{
    [Fact]
    public void CreateProcessSpec_Should_UseProjectDefaultsAndArgumentSafeInputs()
    {
        var projectPath = GetTestProjectFile();
        var schemaPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "schema export",
            "schema.graphqls");

        var spec = GraphQLSchemaExportCommand.CreateProcessSpec(
            projectPath,
            schemaPath,
            "Products Schema");

        $$"""
        Executable: {{spec.ExecutablePath}}
        Working directory: {{spec.WorkingDirectory}}
        Arguments:
        {{string.Join(Environment.NewLine, spec.Arguments)}}
        """.MatchInlineSnapshot(
            $$"""
            Executable: dotnet
            Working directory: {{System.IO.Path.GetDirectoryName(projectPath)}}
            Arguments:
            run
            --project
            {{projectPath}}
            --no-launch-profile
            --
            schema
            export
            --output
            {{schemaPath}}
            --schema-name
            Products Schema
            """);
    }

    [Fact]
    public void CreateProcessSpec_Should_OmitSchemaName_When_NotConfigured()
    {
        var projectPath = GetTestProjectFile();
        var schemaPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "schema.graphqls");

        var spec = GraphQLSchemaExportCommand.CreateProcessSpec(
            projectPath,
            schemaPath,
            schemaName: null);

        Assert.Equal(
            [
                "run",
                "--project",
                projectPath,
                "--no-launch-profile",
                "--",
                "schema",
                "export",
                "--output",
                schemaPath
            ],
            spec.Arguments);
    }

    [Fact]
    public async Task ValidateArtifactsAsync_Should_AcceptExactNameAndValidSchema()
    {
        using var directory = new TemporaryDirectory();
        var schemaPath = directory.WriteFile(
            "schema.graphqls",
            "type Query { product: String }");
        var settingsPath = directory.WriteFile(
            "schema-settings.json",
            """{ "name": "Products" }""");

        await GraphQLSchemaExportCommand.ValidateArtifactsAsync(
            "products",
            "Products",
            schemaPath,
            settingsPath,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public void PrepareSettingsFile_Should_CopyProjectSettingsIntoExportDirectory()
    {
        using var directory = new TemporaryDirectory();
        var projectPath = directory.WriteFile("products.csproj", "<Project />");
        const string expectedSettings =
            """{ "name": "Products", "environments": { "production": { } } }""";
        directory.WriteFile("schema-settings.json", expectedSettings);
        var exportDirectory = directory.CreateDirectory("export");
        var settingsPath = System.IO.Path.Combine(
            exportDirectory,
            "schema-settings.json");

        GraphQLSchemaExportCommand.PrepareSettingsFile(
            projectPath,
            settingsPath);

        Assert.Equal(expectedSettings, File.ReadAllText(settingsPath));
    }

    [Fact]
    public async Task ValidateArtifactsAsync_Should_RejectNameMismatch()
    {
        using var directory = new TemporaryDirectory();
        var schemaPath = directory.WriteFile(
            "schema.graphqls",
            "type Query { product: String }");
        var settingsPath = directory.WriteFile(
            "schema-settings.json",
            """{ "name": "Inventory" }""");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => GraphQLSchemaExportCommand.ValidateArtifactsAsync(
                "products",
                "Products",
                schemaPath,
                settingsPath,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "The configured source schema name 'Products' for resource 'products' "
            + "does not match schema-settings.json name 'Inventory'.",
            exception.Message);
    }

    [Theory]
    [InlineData("  ", "Schema export for resource 'products' produced empty GraphQL SDL.")]
    [InlineData(
        "type Query {",
        "Schema for resource 'products' is not valid GraphQL SDL.")]
    public async Task ValidateArtifactsAsync_Should_RejectInvalidSchema(
        string schema,
        string expectedMessage)
    {
        using var directory = new TemporaryDirectory();
        var schemaPath = directory.WriteFile("schema.graphqls", schema);
        var settingsPath = directory.WriteFile(
            "schema-settings.json",
            """{ "name": "Products" }""");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => GraphQLSchemaExportCommand.ValidateArtifactsAsync(
                "products",
                "Products",
                schemaPath,
                settingsPath,
                TestContext.Current.CancellationToken));

        Assert.Equal(expectedMessage, exception.Message);
    }

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => System.IO.Path.GetFullPath(
            System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(sourceFile)!,
                "HotChocolate.Fusion.Aspire.Tests.csproj"));

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "fusion-aspire-tests",
            Guid.NewGuid().ToString("N"));

        public TemporaryDirectory()
        {
            Directory.CreateDirectory(_path);
        }

        public string WriteFile(string fileName, string content)
        {
            var path = System.IO.Path.Combine(_path, fileName);
            File.WriteAllText(path, content);
            return path;
        }

        public string CreateDirectory(string directoryName)
        {
            var path = System.IO.Path.Combine(_path, directoryName);
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(_path, recursive: true);
        }
    }
}
