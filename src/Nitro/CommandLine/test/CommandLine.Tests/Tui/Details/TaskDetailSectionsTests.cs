using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using Spectre.Console;

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
                "Comments",
                $"alice - {TaskDates.Format(DateTimeOffset.UnixEpoch)}",
                "hello world",
                "",
                $"bob - {TaskDates.Format(DateTimeOffset.UnixEpoch)}",
                "hi"
            ],
            lines);
    }

    [Fact]
    public void WrapLine_Should_NotSplitTextElement_When_WordContainsCombiningMark()
    {
        // act
        var lines = TaskDetailSections.WrapLine("éabc", width: 1);

        // assert
        Assert.Equal("é", lines[0]);
    }

    [Fact]
    public void WrapLine_Should_FitDisplayWidthAndPreserveSurrogatePairs_When_TextContainsCjkAndEmoji()
    {
        // arrange
        const int width = 2;

        // act
        var lines = TaskDetailSections.WrapLine("漢 a😀xyz", width);

        // assert
        Assert.All(lines, line => Assert.True(line.GetCellWidth() <= width));
        Assert.All(lines, line => Assert.False(HasUnpairedSurrogate(line)));
    }

    [Fact]
    public void WrapLine_Should_ReplaceOversizedCjkWithEllipsis_When_WidthIsOne()
    {
        // arrange
        const int width = 1;

        // act
        var lines = TaskDetailSections.WrapLine("漢abc", width);

        // assert
        Assert.Equal(["…", "a", "b", "c"], lines);
        Assert.All(lines, line => Assert.True(line.GetCellWidth() <= width));
        Assert.Equal("abc", string.Concat(lines.Skip(1)));
    }

    [Fact]
    public void WrapLine_Should_ReplaceOversizedEmojiWithEllipsis_When_WidthIsOne()
    {
        // arrange
        const int width = 1;

        // act
        var lines = TaskDetailSections.WrapLine("😀abc", width);

        // assert
        Assert.Equal(["…", "a", "b", "c"], lines);
        Assert.All(lines, line => Assert.True(line.GetCellWidth() <= width));
        Assert.All(lines, line => Assert.False(HasUnpairedSurrogate(line)));
    }

    private static bool HasUnpairedSurrogate(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                {
                    return true;
                }

                i++;
            }
            else if (char.IsLowSurrogate(value[i]))
            {
                return true;
            }
        }

        return false;
    }
}
