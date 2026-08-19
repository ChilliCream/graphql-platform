using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets.Form;

public sealed class TextFieldTests
{
    private static ConsoleKeyInfo Key(char c, ConsoleKey key = ConsoleKey.NoName) =>
        new(c, key, shift: false, alt: false, control: false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    [Fact]
    public void HandleKey_Should_InsertCharacter_When_Typed()
    {
        // arrange
        var field = new TextField("title", "Title");

        // act
        field.HandleKey(Key('h'));
        field.HandleKey(Key('i'));

        // assert
        Assert.Equal(new FormValue.Text("hi"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_InsertAtCursor_When_CursorMovedLeft()
    {
        // arrange
        var field = new TextField("title", "Title", initialValue: "ac");
        field.HandleKey(Key(ConsoleKey.LeftArrow));

        // act
        field.HandleKey(Key('b'));

        // assert
        Assert.Equal(new FormValue.Text("abc"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_RemovePriorCharacter_When_Backspace()
    {
        // arrange
        var field = new TextField("title", "Title", initialValue: "abc");

        // act
        field.HandleKey(Key(ConsoleKey.Backspace));

        // assert
        Assert.Equal(new FormValue.Text("ab"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_RemoveFollowingCharacter_When_Delete()
    {
        // arrange
        var field = new TextField("title", "Title", initialValue: "abc");
        field.HandleKey(Key(ConsoleKey.Home));

        // act
        field.HandleKey(Key(ConsoleKey.Delete));

        // assert
        Assert.Equal(new FormValue.Text("bc"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_UpArrow()
    {
        // arrange
        var field = new TextField("title", "Title", initialValue: "abc");

        // act
        var handled = field.HandleKey(Key(ConsoleKey.UpArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_DownArrow()
    {
        // arrange
        var field = new TextField("title", "Title", initialValue: "abc");

        // act
        var handled = field.HandleKey(Key(ConsoleKey.DownArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void Render_Should_ShowRequiredMarker_When_Required()
    {
        // arrange
        var field = new TextField("title", "Title", required: true);
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: false));

        // assert
        Assert.Contains("Title *", console.Output);
    }

    [Fact]
    public void Render_Should_NotShowValidationError_When_ShowErrorsIsFalse()
    {
        // arrange
        var field = new TextField("title", "Title", validator: _ => "Title is required.");
        var console = new TestConsole().Width(60);

        // act: ShowErrors defaults to false until the hosting Form sets it.
        console.Write(field.Render(40, focused: false));

        // assert
        Assert.DoesNotContain("Title is required.", console.Output);
    }

    [Fact]
    public void Render_Should_ShowValidationError_When_ShowErrorsIsTrue()
    {
        // arrange
        var field = new TextField("title", "Title", validator: _ => "Title is required.")
        {
            ShowErrors = true
        };
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: false));

        // assert
        Assert.Contains("Title is required.", console.Output);
    }

    [Fact]
    public void Render_Should_ScrollHorizontally_When_TextOverflowsWidth()
    {
        // arrange
        var field = new TextField("title", "Title", initialValue: "0123456789abcdefghij");
        field.HandleKey(Key(ConsoleKey.End));
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(14, focused: true));

        // assert: the cursor sits at the end, so the earliest characters have
        // scrolled out of view.
        Assert.DoesNotContain("0123456789", console.Output);
        Assert.Contains("j", console.Output);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthIsZero()
    {
        // arrange
        var field = new TextField("title", "Title", initialValue: "abc");
        var console = new TestConsole().Width(60);

        // act
        var exception = Record.Exception(() => console.Write(field.Render(0, focused: true)));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_Should_ReturnError_When_ValidatorFails()
    {
        // arrange
        var field = new TextField(
            "title",
            "Title",
            validator: value => value is FormValue.Text { Value.Length: 0 } ? "Title is required." : null);

        // act
        var error = field.Validate();

        // assert
        Assert.Equal("Title is required.", error);
    }

    [Fact]
    public void Validate_Should_ReturnNull_When_ValidatorPasses()
    {
        // arrange
        var field = new TextField(
            "title",
            "Title",
            initialValue: "hi",
            validator: value => value is FormValue.Text { Value.Length: 0 } ? "Title is required." : null);

        // act
        var error = field.Validate();

        // assert
        Assert.Null(error);
    }

    [Fact]
    public void Render_Should_ShowPlaceholder_When_EmptyAndUnfocused()
    {
        // arrange
        var field = new TextField("title", "Title");
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: false));

        // assert: an interior line is present instead of a collapsed border pair.
        Assert.Contains("empty", console.Output);
        Assert.Equal(3, console.Output.TrimEnd('\n').Split('\n').Length);
    }

    [Fact]
    public void Render_Should_NotShowPlaceholder_When_EmptyAndFocused()
    {
        // arrange: an active cursor already gives the field an interior line.
        var field = new TextField("title", "Title");
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: true));

        // assert
        Assert.DoesNotContain("empty", console.Output);
    }
}
