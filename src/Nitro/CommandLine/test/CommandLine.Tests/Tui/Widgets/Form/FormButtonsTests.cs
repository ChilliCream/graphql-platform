using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets.Form;

public sealed class FormButtonsTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static FormButtons CreateButtons() => new(
        [
            new FormButtonSpec("save", "Save", ButtonKind.Primary),
            new FormButtonSpec("cancel", "Cancel", ButtonKind.Secondary)
        ]);

    [Fact]
    public void Constructor_Should_Throw_When_NoButtons()
    {
        // act
        var exception = Record.Exception(() => new FormButtons([]));

        // assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void SelectedId_Should_BeFirstButton_Initially()
    {
        // arrange
        var buttons = CreateButtons();

        // assert
        Assert.Equal("save", buttons.SelectedId);
        Assert.Equal(ButtonKind.Primary, buttons.SelectedKind);
    }

    [Fact]
    public void HandleKey_Should_MoveSelection_When_RightArrow()
    {
        // arrange
        var buttons = CreateButtons();

        // act
        var handled = buttons.HandleKey(Key(ConsoleKey.RightArrow));

        // assert
        Assert.True(handled);
        Assert.Equal("cancel", buttons.SelectedId);
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_RightArrowOnLastButton()
    {
        // arrange
        var buttons = CreateButtons();
        buttons.HandleKey(Key(ConsoleKey.RightArrow));

        // act
        var handled = buttons.HandleKey(Key(ConsoleKey.RightArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void HandleKey_Should_ReturnFalse_When_LeftArrowOnFirstButton()
    {
        // arrange
        var buttons = CreateButtons();

        // act
        var handled = buttons.HandleKey(Key(ConsoleKey.LeftArrow));

        // assert
        Assert.False(handled);
    }

    [Fact]
    public void Render_Should_IncludeButtonLabels()
    {
        // arrange
        var buttons = CreateButtons();
        var console = new TestConsole().Width(60);

        // act
        console.Write(buttons.Render(focused: true));

        // assert
        Assert.Contains("Save", console.Output);
        Assert.Contains("Cancel", console.Output);
    }
}
