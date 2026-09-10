using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets;

public sealed class TaskGlyphsTests
{
    [Theory]
    [InlineData(TaskStates.Closed, "✓")]
    [InlineData(TaskStates.InProgress, "●")]
    [InlineData(TaskStates.Open, "○")]
    [InlineData(TaskStates.Deferred, "⏸")]
    [InlineData(TaskStates.Blocked, "⊘")]
    public void Status_Should_ReturnGlyph_ForEachKnownStatus(string status, string expectedGlyph)
    {
        // act
        var glyph = TaskGlyphs.Status(status);

        // assert
        Assert.Equal(expectedGlyph, glyph);
    }

    [Fact]
    public void Status_Should_FallBackToOpenGlyph_When_StatusUnknown()
    {
        // act
        var glyph = TaskGlyphs.Status("custom_state");

        // assert
        Assert.Equal("○", glyph);
    }

    [Fact]
    public void StatusMarkup_Should_WrapGlyphInThemeStyle()
    {
        // act
        var markup = TaskGlyphs.StatusMarkup(TaskStates.Closed);

        // assert
        markup.MatchInlineSnapshot("[green]✓[/]");
    }

    [Fact]
    public void StatusMarkup_Should_ReturnUnwrappedGlyph_When_StatusHasNoStyleToken()
    {
        // act
        var markup = TaskGlyphs.StatusMarkup("custom_state");

        // assert
        Assert.Equal("○", markup);
    }

    [Theory]
    [InlineData(TaskTypes.Bug, "B")]
    [InlineData(TaskTypes.Feature, "F")]
    [InlineData(TaskTypes.Task, "T")]
    [InlineData(TaskTypes.Epic, "E")]
    [InlineData(TaskTypes.Chore, "C")]
    [InlineData(TaskTypes.Docs, "D")]
    [InlineData(TaskTypes.Question, "Q")]
    public void TypeCode_Should_ReturnCode_ForEachKnownType(string type, string expectedCode)
    {
        // act
        var code = TaskGlyphs.TypeCode(type);

        // assert
        Assert.Equal(expectedCode, code);
    }

    [Fact]
    public void TypeCode_Should_FallBackToFirstLetter_When_TypeUnknown()
    {
        // act
        var code = TaskGlyphs.TypeCode("design");

        // assert
        Assert.Equal("D", code);
    }

    [Fact]
    public void TypeCode_Should_ReturnQuestionMark_When_TypeIsEmpty()
    {
        // act
        var code = TaskGlyphs.TypeCode(string.Empty);

        // assert
        Assert.Equal("?", code);
    }

    [Fact]
    public void TypeCodeMarkup_Should_WrapBracketedCodeInThemeStyle()
    {
        // act
        var markup = TaskGlyphs.TypeCodeMarkup(TaskTypes.Bug);

        // assert
        markup.MatchInlineSnapshot("[red3][[B]][/]");
    }

    [Fact]
    public void TypeCodeMarkup_Should_ReturnUnwrappedCode_When_TypeHasNoStyleToken()
    {
        // act
        var markup = TaskGlyphs.TypeCodeMarkup("design");

        // assert
        Assert.Equal("[[D]]", markup);
    }
}
