using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets.Form;

public sealed class SelectFieldTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static SelectField CreateField(string? initialSelectedId = null) => new(
        "status",
        "Status",
        [new SelectOption("open", "Open"), new SelectOption("closed", "Closed"), new SelectOption("blocked", "Blocked")],
        initialSelectedId: initialSelectedId);

    [Fact]
    public void GetValue_Should_ReturnFirstOption_When_NoInitialSelection()
    {
        // arrange
        var field = CreateField();

        // act
        var value = field.GetValue();

        // assert
        Assert.Equal(new FormValue.Text("open"), value);
    }

    [Fact]
    public void GetValue_Should_ReturnInitialSelection_When_Given()
    {
        // arrange
        var field = CreateField(initialSelectedId: "blocked");

        // act
        var value = field.GetValue();

        // assert
        Assert.Equal(new FormValue.Text("blocked"), value);
    }

    [Fact]
    public void HandleKey_Should_MoveSelectionDown_When_DownArrow()
    {
        // arrange
        var field = CreateField();

        // act
        var handled = field.HandleKey(Key(ConsoleKey.DownArrow));

        // assert
        Assert.True(handled);
        Assert.Equal(new FormValue.Text("closed"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_DownArrowOnLastOption()
    {
        // arrange
        var field = CreateField(initialSelectedId: "blocked");

        // act
        var handled = field.HandleKey(Key(ConsoleKey.DownArrow));

        // assert
        Assert.False(handled);
        Assert.Equal(new FormValue.Text("blocked"), field.GetValue());
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_UpArrowOnFirstOption()
    {
        // arrange
        var field = CreateField();

        // act
        var handled = field.HandleKey(Key(ConsoleKey.UpArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void Render_Should_MarkSelectedOption()
    {
        // arrange
        var field = CreateField(initialSelectedId: "closed");
        var console = new TestConsole().Width(60);

        // act
        console.Write(field.Render(40, focused: false));

        // assert
        Assert.Contains("(o) Closed", console.Output);
        Assert.Contains("( ) Open", console.Output);
    }
}
