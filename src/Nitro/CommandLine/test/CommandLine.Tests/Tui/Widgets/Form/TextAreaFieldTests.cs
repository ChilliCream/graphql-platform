using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets.Form;

public sealed class TextAreaFieldTests
{
    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    [Fact]
    public void HandleKey_Should_InsertCharacter_When_Typed()
    {
        // arrange
        var field = new TextAreaField("notes", "Notes");

        // act
        field.HandleKey(Key('h'));
        field.HandleKey(Key('i'));

        // assert
        Assert.Equal(new FormValue.Text("hi"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_SplitLine_When_Enter()
    {
        // arrange
        var field = new TextAreaField("notes", "Notes", initialValue: "ab");
        field.HandleKey(Key(ConsoleKey.Home));
        field.HandleKey(Key(ConsoleKey.RightArrow));

        // act
        field.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Equal(new FormValue.Text("a\nb"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_MergeLines_When_BackspaceAtLineStart()
    {
        // arrange
        var field = new TextAreaField("notes", "Notes", initialValue: "a\nb");
        field.HandleKey(Key(ConsoleKey.Home));

        // act
        field.HandleKey(Key(ConsoleKey.Backspace));

        // assert
        Assert.Equal(new FormValue.Text("ab"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_MoveToPreviousLine_When_UpArrowNotOnFirstLine()
    {
        // arrange: the cursor starts at the end of "b" (column 1), so moving up
        // clamps to the same column on "a" (also column 1, its end).
        var field = new TextAreaField("notes", "Notes", initialValue: "a\nb");

        // act
        var handled = field.HandleKey(Key(ConsoleKey.UpArrow));
        field.HandleKey(Key('x'));

        // assert
        Assert.True(handled);
        Assert.Equal(new FormValue.Text("ax\nb"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_UpArrowOnFirstLine()
    {
        // arrange
        var field = new TextAreaField("notes", "Notes", initialValue: "a\nb");
        field.HandleKey(Key(ConsoleKey.UpArrow));

        // act
        var handled = field.HandleKey(Key(ConsoleKey.UpArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_DownArrowOnLastLine()
    {
        // arrange
        var field = new TextAreaField("notes", "Notes", initialValue: "a\nb");

        // act
        var handled = field.HandleKey(Key(ConsoleKey.DownArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void Render_Should_ScrollVertically_When_MoreLinesThanVisible()
    {
        // arrange
        var field = new TextAreaField("notes", "Notes", initialValue: "1\n2\n3\n4\n5", visibleLines: 2);
        field.HandleKey(Key(ConsoleKey.End));
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: true));

        // assert: the cursor is on the last line, so the first lines scrolled off.
        Assert.DoesNotContain("1", console.Output);
        Assert.Contains("5", console.Output);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthIsZero()
    {
        // arrange
        var field = new TextAreaField("notes", "Notes", initialValue: "a\nb");
        var console = new TestConsole().Width(60);

        // act
        var exception = Record.Exception(() => console.Write(field.Render(0, focused: true)));

        // assert
        Assert.Null(exception);
    }
}
