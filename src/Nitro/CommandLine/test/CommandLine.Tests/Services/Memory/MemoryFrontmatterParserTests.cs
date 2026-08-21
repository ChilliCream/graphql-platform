using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class MemoryFrontmatterParserTests
{
    private const string ValidId = "01hqzxk8xdtd3fk3f0z7c5g8vm";

    [Fact]
    public void TryParse_Should_Succeed_When_FrontmatterIsWellFormed()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: fact
            tags: [foo, bar-baz]
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "The body text.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out var frontmatter, out var failure);

        // assert
        Assert.True(parsed);
        Assert.Null(failure);
        Assert.Equal(1, frontmatter!.Schema);
        Assert.Equal(ValidId, frontmatter.Id);
        Assert.Equal("fact", frontmatter.Type);
        Assert.Equal(["foo", "bar-baz"], frontmatter.Tags);
        Assert.Equal("pascal", frontmatter.CreatedBy);
        Assert.Null(frontmatter.PromotedFrom);
        Assert.Equal("The body text.", frontmatter.Body);
    }

    [Fact]
    public void TryParse_Should_Succeed_When_TagsAreEmpty()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: fact
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out var frontmatter, out _);

        // assert
        Assert.True(parsed);
        Assert.Empty(frontmatter!.Tags);
    }

    [Fact]
    public void TryParse_Should_Succeed_When_PromotedFromIsPresent()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: decision
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            promoted_from: 01hqzxk6xdtd3fk3f0z7c5g8aa
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out var frontmatter, out _);

        // assert
        Assert.True(parsed);
        Assert.Equal("01hqzxk6xdtd3fk3f0z7c5g8aa", frontmatter!.PromotedFrom);
    }

    [Fact]
    public void TryParse_Should_Fail_When_OpeningDelimiterIsMissing()
    {
        // act
        var parsed = MemoryFrontmatterParser.TryParse("no delimiter here", ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.MalformedFrontmatter, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_ClosingDelimiterIsMissing()
    {
        // act
        var parsed = MemoryFrontmatterParser.TryParse("---\nschema: 1\n", ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.MalformedFrontmatter, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_ALineIsNotAKeyValuePair()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: fact
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            this line has no colon
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.MalformedFrontmatter, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_AKeyIsDuplicated()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            schema: 1
            id: {ValidId}
            type: fact
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.MalformedFrontmatter, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_AKeyIsUnknown()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: fact
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            some_unknown_key: value
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.UnknownKey, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_ARequiredKeyIsMissing()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: fact
            tags: []
            created_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.MissingRequiredKey, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_SchemaVersionIsUnsupported()
    {
        // arrange
        var content = Build(
            $"""
            schema: 2
            id: {ValidId}
            type: fact
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.UnsupportedSchemaVersion, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_IdIsNotAWellFormedMemoryId()
    {
        // arrange
        const string malformedId = "not-a-ulid-shaped-id";
        var content = Build(
            $"""
            schema: 1
            id: {malformedId}
            type: fact
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, malformedId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.InvalidValue, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_IdDoesNotMatchTheFilename()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: fact
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(
            content, "01hqzxk8xdtd3fk3f0z7c5g8zz", out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.FilenameIdMismatch, failure!.Reason);
    }

    [Theory]
    [InlineData("not-a-list")]
    [InlineData("[foo,]")]
    [InlineData("[Invalid Tag!]")]
    public void TryParse_Should_Fail_When_TagsAreMalformed(string tagsValue)
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: fact
            tags: {tagsValue}
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.InvalidValue, failure!.Reason);
    }

    [Theory]
    [InlineData("2026-01-10 12:00:00")] // no offset/Z designator
    [InlineData("not-a-timestamp")]
    [InlineData("2026-01-10T12:00:00+01:00")] // not UTC 'Z'
    public void TryParse_Should_Fail_When_CreatedAtIsNotUtcRfc3339(string createdAt)
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type: fact
            tags: []
            created_at: {createdAt}
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.InvalidValue, failure!.Reason);
    }

    [Fact]
    public void TryParse_Should_Fail_When_TypeIsEmpty()
    {
        // arrange
        var content = Build(
            $"""
            schema: 1
            id: {ValidId}
            type:
            tags: []
            created_at: 2026-01-10T12:00:00Z
            updated_at: 2026-01-10T12:00:00Z
            created_by: pascal
            """,
            "Body.");

        // act
        var parsed = MemoryFrontmatterParser.TryParse(content, ValidId, out _, out var failure);

        // assert
        Assert.False(parsed);
        Assert.Equal(MemoryFrontmatterFailureReason.InvalidValue, failure!.Reason);
    }

    private static string Build(string frontmatter, string body) => $"---\n{frontmatter}\n---\n{body}";
}
