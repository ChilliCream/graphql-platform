using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

public sealed class TaskDetailSectionsTests
{
    [Fact]
    public void BuildTextSection_Should_ReturnEmpty_When_TextIsEmpty()
    {
        // act
        var lines = TaskDetailSections.BuildTextSection("Description:", "", 20);

        // assert
        Assert.Empty(lines);
    }

    [Fact]
    public void BuildTextSection_Should_IncludeHeader_Then_WrappedLines()
    {
        // act
        var lines = TaskDetailSections.BuildTextSection("Description:", "one two three four", 11);

        // assert
        Assert.Equal(["Description:", "one two", "three four"], lines);
    }

    [Fact]
    public void BuildTextSection_Should_PreserveParagraphBreaks()
    {
        // act
        var lines = TaskDetailSections.BuildTextSection("Notes:", "first\nsecond", 20);

        // assert
        Assert.Equal(["Notes:", "first", "second"], lines);
    }

    [Fact]
    public void WrapLine_Should_ReturnLineUnwrapped_When_WidthIsZeroOrLess()
    {
        // act
        var lines = TaskDetailSections.WrapLine("one two three", 0);

        // assert
        Assert.Equal(["one two three"], lines);
    }

    [Fact]
    public void WrapLine_Should_ReturnOneEmptyLine_When_LineIsEmpty()
    {
        // act
        var lines = TaskDetailSections.WrapLine("", 10);

        // assert
        Assert.Equal([""], lines);
    }

    [Fact]
    public void WrapLine_Should_HardBreak_When_WordIsLongerThanWidth()
    {
        // act
        var lines = TaskDetailSections.WrapLine("abcdefghij", 4);

        // assert
        Assert.Equal(["abcd", "efgh", "ij"], lines);
    }

    [Fact]
    public void WrapLine_Should_FitWordsExactlyToWidth()
    {
        // act
        var lines = TaskDetailSections.WrapLine("ab cd", 5);

        // assert
        Assert.Equal(["ab cd"], lines);
    }

    [Fact]
    public void BuildCommentsSection_Should_ReturnEmpty_When_NoComments()
    {
        // act
        var lines = TaskDetailSections.BuildCommentsSection([], 20);

        // assert
        Assert.Empty(lines);
    }

    [Fact]
    public void BuildCommentsSection_Should_RenderAuthorTimestampAndWrappedText_WithBlankLineBetween()
    {
        // arrange
        var comments = new List<TaskComment>
        {
            new()
            {
                TaskId = "t-1",
                Author = "alice",
                Text = "hello world",
                CreatedAt = DateTimeOffset.UnixEpoch
            },
            new()
            {
                TaskId = "t-1",
                Author = "bob",
                Text = "hi",
                CreatedAt = DateTimeOffset.UnixEpoch
            }
        };

        // act
        var lines = TaskDetailSections.BuildCommentsSection(comments, 20);

        // assert
        Assert.Equal(
            [
                "Comments:",
                $"alice - {TaskDates.Format(DateTimeOffset.UnixEpoch)}",
                "hello world",
                "",
                $"bob - {TaskDates.Format(DateTimeOffset.UnixEpoch)}",
                "hi"
            ],
            lines);
    }
}
