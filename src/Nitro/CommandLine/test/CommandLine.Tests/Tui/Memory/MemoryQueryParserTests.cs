using ChilliCream.Nitro.CommandLine.Tui.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

public sealed class MemoryQueryParserTests
{
    [Fact]
    public void Parse_Should_ReturnEmptyQuery_When_InputIsBlank()
    {
        // act
        var query = MemoryQueryParser.Parse("   ");

        // assert
        Assert.True(query.IsEmpty);
    }

    [Fact]
    public void Parse_Should_TreatPlainWords_AsFreeText()
    {
        // act
        var query = MemoryQueryParser.Parse("deploy checklist");

        // assert
        Assert.Equal("deploy checklist", query.Text);
        Assert.Null(query.Type);
        Assert.Empty(query.Tags);
    }

    [Fact]
    public void Parse_Should_ExtractATypeToken_NormalizedLowercase()
    {
        // act
        var query = MemoryQueryParser.Parse("type:Decision");

        // assert
        Assert.Equal("decision", query.Type);
        Assert.Equal("", query.Text);
    }

    [Fact]
    public void Parse_Should_ExtractRepeatedTagTokens_NormalizedLowercase()
    {
        // act
        var query = MemoryQueryParser.Parse("tag:Ops tag:URGENT");

        // assert
        Assert.Equal(["ops", "urgent"], query.Tags);
    }

    [Fact]
    public void Parse_Should_CombineTagTypeAndFreeText()
    {
        // act
        var query = MemoryQueryParser.Parse("deploy tag:ops type:decision checklist");

        // assert
        Assert.Equal("deploy checklist", query.Text);
        Assert.Equal("decision", query.Type);
        Assert.Equal(["ops"], query.Tags);
    }

    [Fact]
    public void Parse_Should_TreatABareTagOrTypePrefix_AsFreeText()
    {
        // act
        var query = MemoryQueryParser.Parse("tag: type:");

        // assert
        Assert.Equal("tag: type:", query.Text);
        Assert.Null(query.Type);
        Assert.Empty(query.Tags);
    }
}
