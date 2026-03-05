using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Tools;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Validation;

public sealed class ValidateClientToolFileResolutionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalDir;

    public ValidateClientToolFileResolutionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "validate-client-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    [Fact]
    public void ResolveFiles_Finds_GraphQL_Files_In_Current_Directory()
    {
        // arrange
        File.WriteAllText(Path.Combine(_tempDir, "query1.graphql"), "{ users { id } }");
        File.WriteAllText(Path.Combine(_tempDir, "query2.graphql"), "{ posts { id } }");

        // act
        var result = InvokeResolveFiles(["*.graphql"]);

        // assert
        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.EndsWith(".graphql", f));
    }

    [Fact]
    public void ResolveFiles_Recursive_Glob_Finds_Nested_Files()
    {
        // arrange
        var subDir = Path.Combine(_tempDir, "src", "queries");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "deep.graphql"), "{ deep { id } }");
        File.WriteAllText(Path.Combine(_tempDir, "root.graphql"), "{ root { id } }");

        // act
        var result = InvokeResolveFiles(["**/*.graphql"]);

        // assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.Contains("deep.graphql"));
        Assert.Contains(result, f => f.Contains("root.graphql"));
    }

    [Fact]
    public void ResolveFiles_Only_Returns_GraphQL_Files()
    {
        // arrange
        File.WriteAllText(Path.Combine(_tempDir, "schema.graphql"), "type Query { id: ID }");
        File.WriteAllText(Path.Combine(_tempDir, "data.json"), "{}");
        File.WriteAllText(Path.Combine(_tempDir, "notes.txt"), "hello");

        // act
        var result = InvokeResolveFiles(["*.*"]);

        // assert
        Assert.Single(result);
        Assert.EndsWith(".graphql", result[0]);
    }

    [Fact]
    public void ResolveFiles_Path_Traversal_Rejected()
    {
        // arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid());
        Directory.CreateDirectory(outsideDir);
        File.WriteAllText(Path.Combine(outsideDir, "secret.graphql"), "secret");

        try
        {
            // act - try path traversal relative to cwd
            var result = InvokeResolveFiles(
                [Path.Combine("..", Path.GetFileName(outsideDir), "secret.graphql")]);

            // assert - path outside workspace root should be rejected
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(outsideDir, true);
        }
    }

    [Fact]
    public void ResolveFiles_Recursive_Glob_Path_Traversal_Rejected()
    {
        // arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "outside-glob-" + Guid.NewGuid());
        Directory.CreateDirectory(outsideDir);
        File.WriteAllText(Path.Combine(outsideDir, "secret.graphql"), "secret");

        try
        {
            // act
            var result = InvokeResolveFiles(
                [Path.Combine("..", Path.GetFileName(outsideDir), "**", "*.graphql")]);

            // assert
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(outsideDir, true);
        }
    }

    [Fact]
    public void ResolveFiles_NonExistent_Path_Returns_Empty()
    {
        // act
        var result = InvokeResolveFiles(["nonexistent/dir/*.graphql"]);

        // assert
        Assert.Empty(result);
    }

    [Fact]
    public void ResolveFiles_Absolute_Path_File()
    {
        // arrange
        var file = Path.Combine(_tempDir, "abs.graphql");
        File.WriteAllText(file, "{ abs { id } }");

        // act
        var result = InvokeResolveFiles([file]);

        // assert
        Assert.Single(result);
        Assert.Equal(file, result[0]);
    }

    [Fact]
    public void ResolveFiles_Deduplicates_Files()
    {
        // arrange
        var file = Path.Combine(_tempDir, "dup.graphql");
        File.WriteAllText(file, "{ dup { id } }");

        // act
        var result = InvokeResolveFiles(["*.graphql", "dup.graphql"]);

        // assert - the file matched by both patterns should appear only once
        Assert.Single(result);
    }

    [Fact]
    public void ResolveFiles_Empty_Patterns_Returns_Empty()
    {
        // act
        var result = InvokeResolveFiles([]);

        // assert
        Assert.Empty(result);
    }

    private static List<string> InvokeResolveFiles(string[] patterns)
    {
        return ValidateClientTool.ResolveFiles(patterns);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDir);

        try
        {
            Directory.Delete(_tempDir, true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
