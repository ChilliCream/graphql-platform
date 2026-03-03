using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.FusionInfo;

public sealed class FusionInfoServiceTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    [Theory]
    [InlineData("my-api", "my-api")]
    [InlineData("My API", "my-api")]
    [InlineData("my_api_v2", "my-api-v2")]
    [InlineData("Hello World!", "hello-world")]
    [InlineData("api@v1.0", "api-v1-0")]
    public void SanitizeName_Replaces_Special_Chars_And_Lowercases(
        string input, string expected)
    {
        // act
        var result = FusionInfoService.SanitizeName(input);

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeName_Truncates_At_40_Characters()
    {
        // arrange
        var longName = new string('a', 60);

        // act
        var result = FusionInfoService.SanitizeName(longName);

        // assert
        Assert.Equal(40, result.Length);
        Assert.Equal(new string('a', 40), result);
    }

    [Fact]
    public void SanitizeName_Short_Name_Not_Truncated()
    {
        // act
        var result = FusionInfoService.SanitizeName("short");

        // assert
        Assert.Equal("short", result);
    }

    [Fact]
    public void SanitizeName_Empty_String_Returns_Empty()
    {
        // act
        var result = FusionInfoService.SanitizeName("");

        // assert
        Assert.Equal("", result);
    }

    [Fact]
    public void SanitizeName_AllSpecialChars_Returns_Empty_After_Trim()
    {
        // act - all special chars become dashes, then trimmed
        var result = FusionInfoService.SanitizeName("@#$%");

        // assert - after trimming leading/trailing dashes, should be empty
        Assert.Equal("", result);
    }

    [Fact]
    public void SanitizeName_Trims_Leading_And_Trailing_Dashes()
    {
        // act
        var result = FusionInfoService.SanitizeName("--hello--");

        // assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void IsCacheValid_Returns_False_For_Missing_File()
    {
        // arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // act
        var result = FusionInfoService.IsCacheValid(nonExistentFile, "tag1");

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsCacheValid_Returns_True_For_Matching_Tag()
    {
        // arrange
        var cacheFile = CreateTempFile();
        File.WriteAllText(cacheFile, "my-tag-123");

        // act
        var result = FusionInfoService.IsCacheValid(cacheFile, "my-tag-123");

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsCacheValid_Returns_False_For_Mismatched_Tag()
    {
        // arrange
        var cacheFile = CreateTempFile();
        File.WriteAllText(cacheFile, "old-tag");

        // act
        var result = FusionInfoService.IsCacheValid(cacheFile, "new-tag");

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsCacheValid_Trims_Whitespace_From_Cached_Tag()
    {
        // arrange
        var cacheFile = CreateTempFile();
        File.WriteAllText(cacheFile, "  my-tag  \n");

        // act
        var result = FusionInfoService.IsCacheValid(cacheFile, "my-tag");

        // assert
        Assert.True(result);
    }

    [Fact]
    public void GetExtractionDirectory_Contains_Sanitized_Names()
    {
        // act
        var result = FusionInfoService.GetExtractionDirectory("My API", "Production");

        // assert
        Assert.Contains("nitro-fusion-", result);
        Assert.Contains("my-api", result);
        Assert.Contains("production", result);
        Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), result);
    }

    [Fact]
    public void GetExtractionDirectory_Produces_Valid_Path()
    {
        // act
        var result = FusionInfoService.GetExtractionDirectory("test-api", "dev");

        // assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.DoesNotContain(" ", Path.GetFileName(result));
    }

    private string CreateTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        File.WriteAllText(path, "");
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
