using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets.Form;

public sealed class EditableListFieldTests
{
    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    [Fact]
    public void GetValue_Should_ReturnInitialValues()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug", "urgent"]);

        // act
        var value = field.GetValue();

        // assert
        Assert.Equal(new FormValue.List(["bug", "urgent"]), value);
    }

    [Fact]
    public void HandleKey_Should_AddEntry_When_A()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug"]);

        // act
        field.HandleKey(Key('a'));
        field.HandleKey(Key('x'));
        field.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Equal(new FormValue.List(["bug", "x"]), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_RemoveSelectedEntry_When_D()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug", "urgent"]);

        // act
        field.HandleKey(Key('d'));

        // assert
        Assert.Equal(new FormValue.List(["urgent"]), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_EditSelectedEntry_When_Enter()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug"]);

        // act
        field.HandleKey(Key(ConsoleKey.Enter));
        field.HandleKey(Key(ConsoleKey.End));
        field.HandleKey(Key('!'));
        field.HandleKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Equal(new FormValue.List(["bug!"]), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_RevertEdit_When_Escape()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug"]);
        field.HandleKey(Key(ConsoleKey.Enter));
        field.HandleKey(Key(ConsoleKey.End));
        field.HandleKey(Key('!'));

        // act
        var handled = field.HandleKey(Key(ConsoleKey.Escape));

        // assert: Escape is consumed by the in-progress edit, not the form.
        Assert.True(handled);
        Assert.Equal(new FormValue.List(["bug"]), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_EscapeWhileBrowsing()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug"]);

        // act
        var handled = field.HandleKey(Key(ConsoleKey.Escape));

        // assert: browsing (not editing) leaves Escape for the form to cancel.
        Assert.False(handled);
    }

    [Fact]
    public void HandleKey_Should_RemoveNewEntry_When_EscapeCancelsIt()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug"]);
        field.HandleKey(Key('a'));
        field.HandleKey(Key('x'));

        // act
        field.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.Equal(new FormValue.List(["bug"]), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_UpArrowOnFirstEntry()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug", "urgent"]);

        // act
        var handled = field.HandleKey(Key(ConsoleKey.UpArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_DownArrowOnLastEntry()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug", "urgent"]);
        field.HandleKey(Key(ConsoleKey.DownArrow));

        // act
        var handled = field.HandleKey(Key(ConsoleKey.DownArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void Render_Should_NotThrow_When_Empty()
    {
        // arrange
        var field = new EditableListField("labels", "Labels");
        var console = new TestConsole().Width(60);

        // act
        var exception = Record.Exception(() => console.Write(field.Render(40, focused: true)));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Render_Should_ListEntries()
    {
        // arrange
        var field = new EditableListField("labels", "Labels", initialValues: ["bug", "urgent"]);
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: false));

        // assert
        Assert.Contains("bug", console.Output);
        Assert.Contains("urgent", console.Output);
    }
}
